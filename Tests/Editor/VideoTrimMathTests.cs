using System;
using System.Collections.Generic;
using NUnit.Framework;
using SOSXR.EditorSpice.EditorScripts;


namespace SOSXR.EditorSpice.Tests
{
    public class VideoTrimMathTests
    {
        [TestCase(10d, 2d, 5d, 2d, 5d)]
        [TestCase(10d, -1d, 20d, 0d, 10d)]
        [TestCase(10d, 0d, 0d, 0d, 10d)] // Default Vector2: whole clip
        [TestCase(10d, 4d, 3d, 4d, 10d)] // End before start: until the end
        [TestCase(10d, 15d, 20d, 10d, 10d)]
        [TestCase(0d, 1d, 2d, 0d, 0d)]
        public void ClampTrim_ClampsToClip(double length, double start, double end, double expectedStart, double expectedEnd)
        {
            VideoTrimMath.ClampTrim(length, start, end, out var clampedStart, out var clampedEnd);

            Assert.AreEqual(expectedStart, clampedStart, 1e-9);
            Assert.AreEqual(expectedEnd, clampedEnd, 1e-9);
        }


        [TestCase(0d, 30d, 0L)]
        [TestCase(1d, 30d, 30L)]
        [TestCase(0.1d, 30d, 3L)] // 0.1 * 30 is 2.9999..: still frame 3
        [TestCase(1.5d, 29.97d, 44L)]
        [TestCase(-1d, 30d, 0L)]
        [TestCase(1d, 0d, 0L)]
        public void TimeToFrame_FloorsWithTolerance(double seconds, double frameRate, long expected)
        {
            Assert.AreEqual(expected, VideoTrimMath.TimeToFrame(seconds, frameRate));
        }


        [TestCase(1920, 1920)]
        [TestCase(1081, 1080)]
        [TestCase(1, 2)]
        [TestCase(0, 2)]
        public void MakeEven_RoundsDownToEven(int value, int expected)
        {
            Assert.AreEqual(expected, VideoTrimMath.MakeEven(value));
        }


        [Test]
        public void GetOutputPath_FreeName_UsesSimpleName()
        {
            var path = VideoTrimMath.GetOutputPathWithoutExtension("Assets/Videos/", "Clip", ".mp4", _ => false, DateTime.Now);

            Assert.AreEqual("Assets/Videos/Clip_Trimmed", path);
        }


        [Test]
        public void GetOutputPath_ExistingFile_AddsTimestamp()
        {
            var existing = new HashSet<string> {"Assets/Clip_Trimmed.mp4"};
            var now = new DateTime(2026, 11, 13, 9, 30, 15);

            var path = VideoTrimMath.GetOutputPathWithoutExtension("Assets", "Clip", "mp4", existing.Contains, now);

            Assert.AreEqual("Assets/Clip_Trimmed_2026-11-13_09-30-15", path);
        }


        [Test]
        public void GetOutputPath_TimestampAlsoTaken_AddsCounter()
        {
            var existing = new HashSet<string> {"A/Clip_Trimmed.mp4", "A/Clip_Trimmed_2026-11-13_09-30-15.mp4"};
            var now = new DateTime(2026, 11, 13, 9, 30, 15);

            var path = VideoTrimMath.GetOutputPathWithoutExtension(@"A\", "Clip", ".mp4", existing.Contains, now);

            Assert.AreEqual("A/Clip_Trimmed_2026-11-13_09-30-15_2", path);
        }


        [Test]
        public void GetOutputPath_NullExistsCheck_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => VideoTrimMath.GetOutputPathWithoutExtension("A", "B", ".mp4", null, DateTime.Now));
        }
    }
}
