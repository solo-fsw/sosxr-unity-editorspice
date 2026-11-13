using System;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Sample math used by the <see cref="AudioEditor" />: trimming, fading, waveform peaks and time formatting.
    ///     Works on interleaved sample arrays (as returned by AudioClip.GetData), so it has no Unity dependencies.
    ///     A 'frame' is one sample for every channel.
    /// </summary>
    public static class AudioClipProcessor
    {
        /// <summary>
        ///     Converts a time in seconds to a frame index, clamped to [0, totalFrames].
        /// </summary>
        public static int TimeToFrame(float seconds, int frequency, int totalFrames)
        {
            if (frequency <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency), "Frequency must be positive.");
            }

            if (float.IsNaN(seconds) || seconds <= 0f)
            {
                return 0;
            }

            var frame = Math.Round(seconds * (double) frequency);

            return frame >= totalFrames ? Math.Max(totalFrames, 0) : (int) frame;
        }


        /// <summary>
        ///     Converts a duration in seconds to a frame count, clamped to [0, maxFrames].
        /// </summary>
        public static int SecondsToFrames(float seconds, int frequency, int maxFrames)
        {
            return TimeToFrame(seconds, frequency, maxFrames);
        }


        /// <summary>
        ///     Clamps trim and fade values so that 0 &lt;= start &lt;= end &lt;= length and fadeIn + fadeOut &lt;= end - start.
        ///     Fade in wins over fade out when both do not fit.
        /// </summary>
        public static void ClampEdit(float length, ref float start, ref float end, ref float fadeIn, ref float fadeOut)
        {
            length = Math.Max(0f, length);
            start = Clamp(start, 0f, length);
            end = Clamp(end, start, length);

            var duration = end - start;
            fadeIn = Clamp(fadeIn, 0f, duration);
            fadeOut = Clamp(fadeOut, 0f, duration - fadeIn);
        }


        /// <summary>
        ///     Linear fade gain for a frame in a clip of frameCount frames. Returns 1 outside the fade regions.
        ///     The first frame of a fade in and the last frame of a fade out are silent.
        /// </summary>
        public static float FadeGain(int frame, int frameCount, int fadeInFrames, int fadeOutFrames)
        {
            var gain = 1f;

            if (fadeInFrames > 0 && frame < fadeInFrames)
            {
                gain *= (float) frame / fadeInFrames;
            }

            var framesFromEnd = frameCount - 1 - frame;

            if (fadeOutFrames > 0 && framesFromEnd < fadeOutFrames)
            {
                gain *= (float) framesFromEnd / fadeOutFrames;
            }

            return Clamp(gain, 0f, 1f);
        }


        /// <summary>
        ///     Copies frames [startFrame, endFrame) of an interleaved buffer into a new array.
        /// </summary>
        public static float[] Trim(float[] interleaved, int channels, int startFrame, int endFrame)
        {
            ValidateBuffer(interleaved, channels);

            var totalFrames = interleaved.Length / channels;
            startFrame = Clamp(startFrame, 0, totalFrames);
            endFrame = Clamp(endFrame, startFrame, totalFrames);

            var result = new float[(endFrame - startFrame) * channels];
            Array.Copy(interleaved, startFrame * channels, result, 0, result.Length);

            return result;
        }


        /// <summary>
        ///     Applies a linear fade in and fade out, in place. All channels of a frame get the same gain.
        /// </summary>
        public static void ApplyFades(float[] interleaved, int channels, int fadeInFrames, int fadeOutFrames)
        {
            ValidateBuffer(interleaved, channels);

            var frameCount = interleaved.Length / channels;
            fadeInFrames = Clamp(fadeInFrames, 0, frameCount);
            fadeOutFrames = Clamp(fadeOutFrames, 0, frameCount);

            if (fadeInFrames == 0 && fadeOutFrames == 0)
            {
                return;
            }

            for (var frame = 0; frame < frameCount; frame++)
            {
                if (frame >= fadeInFrames && frame < frameCount - fadeOutFrames)
                {
                    frame = frameCount - fadeOutFrames - 1; // Skip the untouched middle part

                    continue;
                }

                var gain = FadeGain(frame, frameCount, fadeInFrames, fadeOutFrames);

                for (var channel = 0; channel < channels; channel++)
                {
                    interleaved[frame * channels + channel] *= gain;
                }
            }
        }


        /// <summary>
        ///     Trims the buffer to [startTime, endTime] and applies the fades. The source buffer is not changed.
        /// </summary>
        public static float[] Process(float[] interleaved, int channels, int frequency, float startTime, float endTime, float fadeInSeconds, float fadeOutSeconds)
        {
            ValidateBuffer(interleaved, channels);

            var totalFrames = interleaved.Length / channels;
            var startFrame = TimeToFrame(startTime, frequency, totalFrames);
            var endFrame = Math.Max(startFrame, TimeToFrame(endTime, frequency, totalFrames));

            var result = Trim(interleaved, channels, startFrame, endFrame);
            var frameCount = endFrame - startFrame;

            ApplyFades(
                result,
                channels,
                SecondsToFrames(fadeInSeconds, frequency, frameCount),
                SecondsToFrames(fadeOutSeconds, frequency, frameCount)
            );

            return result;
        }


        /// <summary>
        ///     Splits frames [startFrame, endFrame) into equal buckets and returns the peak absolute amplitude of each, over all channels.
        ///     When there are fewer frames than buckets, frames are repeated.
        /// </summary>
        public static float[] ComputePeaks(float[] interleaved, int channels, int startFrame, int endFrame, int buckets)
        {
            ValidateBuffer(interleaved, channels);

            if (buckets <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(buckets), "Bucket count must be positive.");
            }

            var peaks = new float[buckets];
            var totalFrames = interleaved.Length / channels;
            startFrame = Clamp(startFrame, 0, totalFrames);
            endFrame = Clamp(endFrame, startFrame, totalFrames);
            var range = endFrame - startFrame;

            if (range == 0)
            {
                return peaks;
            }

            for (var bucket = 0; bucket < buckets; bucket++)
            {
                var from = startFrame + (int) ((long) range * bucket / buckets);
                var to = startFrame + (int) ((long) range * (bucket + 1) / buckets);
                to = Math.Min(Math.Max(to, from + 1), endFrame);

                var peak = 0f;

                for (var i = from * channels; i < to * channels; i++)
                {
                    var value = Math.Abs(interleaved[i]);

                    if (value > peak)
                    {
                        peak = value;
                    }
                }

                peaks[bucket] = peak;
            }

            return peaks;
        }


        /// <summary>
        ///     Interval between timeline markers so that a range of this duration gets a readable number of ticks.
        /// </summary>
        public static float CalculateMarkerInterval(float duration)
        {
            if (duration <= 1f)
            {
                return 0.1f;
            }

            if (duration <= 5f)
            {
                return 0.5f;
            }

            if (duration <= 10f)
            {
                return 1f;
            }

            if (duration <= 30f)
            {
                return 5f;
            }

            return duration <= 60f ? 10f : 30f;
        }


        /// <summary>
        ///     Formats seconds as "ss.fff", or "mm:ss.fff" from one minute on. Negative values show as zero.
        /// </summary>
        public static string FormatTime(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0f)
            {
                seconds = 0f;
            }

            var totalMilliseconds = (long) Math.Round(seconds * 1000d);
            var minutes = totalMilliseconds / 60000;
            var secs = totalMilliseconds / 1000 % 60;
            var milliseconds = totalMilliseconds % 1000;

            return minutes > 0 ? $"{minutes:00}:{secs:00}.{milliseconds:000}" : $"{secs:00}.{milliseconds:000}";
        }


        private static void ValidateBuffer(float[] interleaved, int channels)
        {
            if (interleaved == null)
            {
                throw new ArgumentNullException(nameof(interleaved));
            }

            if (channels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channels), "Channel count must be positive.");
            }
        }


        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }


        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
