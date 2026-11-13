using System;
using NUnit.Framework;
using SOSXR.EditorSpice.EditorScripts;


namespace SOSXR.EditorSpice.Tests
{
    public class AudioClipProcessorTests
    {
        private const float Tolerance = 1e-5f;


        private static float[] Ones(int frames, int channels)
        {
            var samples = new float[frames * channels];

            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = 1f;
            }

            return samples;
        }


        [TestCase(0f, 0)]
        [TestCase(-1f, 0)]
        [TestCase(0.5f, 50)]
        [TestCase(1f, 100)]
        [TestCase(5f, 100)]
        [TestCase(0.29f, 29)] // 0.29f * 100 is 28.99..: rounds instead of truncating float error
        public void TimeToFrame_ConvertsAndClamps(float seconds, int expected)
        {
            Assert.AreEqual(expected, AudioClipProcessor.TimeToFrame(seconds, 100, 100));
        }


        [Test]
        public void TimeToFrame_NonPositiveFrequency_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioClipProcessor.TimeToFrame(1f, 0, 100));
        }


        [Test]
        public void ClampEdit_KeepsRangeInsideClip()
        {
            float start = -1f, end = 20f, fadeIn = 0f, fadeOut = 0f;

            AudioClipProcessor.ClampEdit(10f, ref start, ref end, ref fadeIn, ref fadeOut);

            Assert.AreEqual(0f, start);
            Assert.AreEqual(10f, end);
        }


        [Test]
        public void ClampEdit_EndBeforeStart_MovesEndToStart()
        {
            float start = 6f, end = 2f, fadeIn = 0f, fadeOut = 0f;

            AudioClipProcessor.ClampEdit(10f, ref start, ref end, ref fadeIn, ref fadeOut);

            Assert.AreEqual(6f, start);
            Assert.AreEqual(6f, end);
        }


        [Test]
        public void ClampEdit_FadesNeverExceedDuration_FadeInWins()
        {
            float start = 2f, end = 5f, fadeIn = 2f, fadeOut = 2f;

            AudioClipProcessor.ClampEdit(10f, ref start, ref end, ref fadeIn, ref fadeOut);

            Assert.AreEqual(2f, fadeIn);
            Assert.AreEqual(1f, fadeOut, Tolerance);
        }


        [Test]
        public void FadeGain_IsLinearAndOneInTheMiddle()
        {
            Assert.AreEqual(0f, AudioClipProcessor.FadeGain(0, 100, 10, 10), Tolerance);
            Assert.AreEqual(0.5f, AudioClipProcessor.FadeGain(5, 100, 10, 10), Tolerance);
            Assert.AreEqual(1f, AudioClipProcessor.FadeGain(50, 100, 10, 10), Tolerance);
            Assert.AreEqual(0.5f, AudioClipProcessor.FadeGain(94, 100, 10, 10), Tolerance);
            Assert.AreEqual(0f, AudioClipProcessor.FadeGain(99, 100, 10, 10), Tolerance);
        }


        [Test]
        public void FadeGain_NoFades_IsOne()
        {
            Assert.AreEqual(1f, AudioClipProcessor.FadeGain(0, 100, 0, 0));
            Assert.AreEqual(1f, AudioClipProcessor.FadeGain(99, 100, 0, 0));
        }


        [Test]
        public void Trim_CopiesWholeFramesOnly()
        {
            var samples = new float[] {0, 0, 1, 1, 2, 2, 3, 3}; // 4 stereo frames

            var result = AudioClipProcessor.Trim(samples, 2, 1, 3);

            CollectionAssert.AreEqual(new float[] {1, 1, 2, 2}, result);
        }


        [Test]
        public void Trim_OutOfRange_IsClamped()
        {
            var samples = new float[] {0, 1, 2, 3};

            CollectionAssert.AreEqual(new float[] {2, 3}, AudioClipProcessor.Trim(samples, 1, 2, 99));
            CollectionAssert.IsEmpty(AudioClipProcessor.Trim(samples, 1, 3, 1));
        }


        [Test]
        public void ApplyFades_AppliesSameGainToAllChannelsOfAFrame()
        {
            var samples = Ones(10, 2);

            AudioClipProcessor.ApplyFades(samples, 2, 4, 0);

            for (var frame = 0; frame < 10; frame++)
            {
                Assert.AreEqual(samples[frame * 2], samples[frame * 2 + 1], "Frame " + frame);
            }

            Assert.AreEqual(0f, samples[0], Tolerance);
            Assert.AreEqual(0.5f, samples[4], Tolerance); // Frame 2 of a 4-frame fade
            Assert.AreEqual(1f, samples[8], Tolerance); // Frame 4: after the fade
        }


        [Test]
        public void ApplyFades_FadeOutEndsSilentAndLeavesMiddleUntouched()
        {
            var samples = Ones(10, 1);

            AudioClipProcessor.ApplyFades(samples, 1, 0, 4);

            Assert.AreEqual(1f, samples[5], Tolerance);
            Assert.AreEqual(0.75f, samples[6], Tolerance);
            Assert.AreEqual(0f, samples[9], Tolerance);
        }


        [Test]
        public void ApplyFades_FadeLongerThanBuffer_DoesNotThrow()
        {
            var samples = Ones(3, 1);

            Assert.DoesNotThrow(() => AudioClipProcessor.ApplyFades(samples, 1, 100, 100));
            Assert.AreEqual(0f, samples[0], Tolerance);
        }


        [Test]
        public void Process_TrimsByTimeAndKeepsSourceIntact()
        {
            var samples = new float[200]; // 100 stereo frames at 100 Hz = 1 second

            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = i;
            }

            var result = AudioClipProcessor.Process(samples, 2, 100, 0.25f, 0.75f, 0f, 0f);

            Assert.AreEqual(100, result.Length); // 50 frames * 2 channels
            Assert.AreEqual(50f, result[0]); // Frame 25, left channel
            Assert.AreEqual(1f, samples[1]); // Source not changed
        }


        [Test]
        public void Process_AppliesFadesRelativeToTrimmedRange()
        {
            var samples = Ones(100, 1);

            var result = AudioClipProcessor.Process(samples, 1, 100, 0.5f, 1f, 0.1f, 0.1f);

            Assert.AreEqual(50, result.Length);
            Assert.AreEqual(0f, result[0], Tolerance);
            Assert.AreEqual(1f, result[25], Tolerance);
            Assert.AreEqual(0f, result[49], Tolerance);
        }


        [Test]
        public void Process_EndBeforeStart_ReturnsEmpty()
        {
            CollectionAssert.IsEmpty(AudioClipProcessor.Process(Ones(100, 1), 1, 100, 0.8f, 0.2f, 0f, 0f));
        }


        [Test]
        public void ComputePeaks_ReturnsMaxAbsolutePerBucketOverAllChannels()
        {
            var samples = new float[] {0.1f, -0.9f, 0.2f, 0.3f, -0.4f, 0.1f, 0.5f, 0f}; // 4 stereo frames

            var peaks = AudioClipProcessor.ComputePeaks(samples, 2, 0, 4, 2);

            Assert.AreEqual(0.9f, peaks[0], Tolerance);
            Assert.AreEqual(0.5f, peaks[1], Tolerance);
        }


        [Test]
        public void ComputePeaks_MoreBucketsThanFrames_RepeatsFrames()
        {
            var peaks = AudioClipProcessor.ComputePeaks(new[] {0.5f, 1f}, 1, 0, 2, 4);

            CollectionAssert.AreEqual(new[] {0.5f, 0.5f, 1f, 1f}, peaks);
        }


        [Test]
        public void ComputePeaks_EmptyRange_ReturnsZeros()
        {
            CollectionAssert.AreEqual(new float[3], AudioClipProcessor.ComputePeaks(new[] {1f, 1f}, 1, 1, 1, 3));
        }


        [TestCase(0.5f, 0.1f)]
        [TestCase(3f, 0.5f)]
        [TestCase(8f, 1f)]
        [TestCase(20f, 5f)]
        [TestCase(45f, 10f)]
        [TestCase(300f, 30f)]
        public void CalculateMarkerInterval_ScalesWithDuration(float duration, float expected)
        {
            Assert.AreEqual(expected, AudioClipProcessor.CalculateMarkerInterval(duration));
        }


        [TestCase(0f, "00.000")]
        [TestCase(1.5f, "01.500")]
        [TestCase(59.9999f, "01:00.000")]
        [TestCase(75.25f, "01:15.250")]
        [TestCase(-3f, "00.000")]
        public void FormatTime_FormatsSecondsAndMinutes(float seconds, string expected)
        {
            Assert.AreEqual(expected, AudioClipProcessor.FormatTime(seconds));
        }


        [Test]
        public void InvalidBuffer_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AudioClipProcessor.Trim(null, 1, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioClipProcessor.Trim(new float[2], 0, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioClipProcessor.ComputePeaks(new float[2], 1, 0, 1, 0));
        }
    }
}
