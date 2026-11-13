using System;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Trim math and output naming for the <see cref="VideoEditor" /> window. Has no Unity dependencies.
    /// </summary>
    public static class VideoTrimMath
    {
        /// <summary>
        ///     Clamps a trim range to [0, length]. An end that is not after the start means: until the end of the clip.
        /// </summary>
        public static void ClampTrim(double length, double start, double end, out double clampedStart, out double clampedEnd)
        {
            if (double.IsNaN(length) || length < 0d)
            {
                length = 0d;
            }

            clampedStart = double.IsNaN(start) ? 0d : Math.Min(Math.Max(start, 0d), length);
            clampedEnd = double.IsNaN(end) || end <= clampedStart ? length : Math.Min(end, length);
        }


        /// <summary>
        ///     Frame index at a time in seconds. Small floating point errors round to the nearest frame instead of down.
        /// </summary>
        public static long TimeToFrame(double seconds, double frameRate)
        {
            if (frameRate <= 0d || double.IsNaN(seconds) || seconds <= 0d)
            {
                return 0;
            }

            return (long) Math.Floor(seconds * frameRate + 1e-6);
        }


        /// <summary>
        ///     Rounds a pixel size down to an even number, at least 2. H.264 (MP4) needs even frame sizes.
        /// </summary>
        public static int MakeEven(int value)
        {
            return Math.Max(2, value - value % 2);
        }


        /// <summary>
        ///     Output path without extension: "{directory}/{clipName}_Trimmed", or with a timestamp when that file already exists.
        ///     The Recorder adds the extension itself.
        /// </summary>
        public static string GetOutputPathWithoutExtension(string directory, string clipName, string extension, Func<string, bool> fileExists, DateTime now)
        {
            if (fileExists == null)
            {
                throw new ArgumentNullException(nameof(fileExists));
            }

            directory = string.IsNullOrEmpty(directory) ? "." : directory.Replace('\\', '/').TrimEnd('/');
            clipName = string.IsNullOrEmpty(clipName) ? "Video" : clipName;
            extension = string.IsNullOrEmpty(extension) || extension.StartsWith(".") ? extension ?? string.Empty : "." + extension;

            var path = $"{directory}/{clipName}_Trimmed";

            if (!fileExists(path + extension))
            {
                return path;
            }

            var stamped = $"{path}_{now:yyyy-MM-dd_HH-mm-ss}";
            var candidate = stamped;

            for (var i = 2; fileExists(candidate + extension); i++)
            {
                candidate = $"{stamped}_{i}";
            }

            return candidate;
        }
    }
}
