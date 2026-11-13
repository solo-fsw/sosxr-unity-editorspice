using System;
using System.Text;
using NUnit.Framework;


namespace SOSXR.EditorSpice.Tests
{
    public class WavUtilityTests
    {
        private static string Ascii(byte[] bytes, int offset)
        {
            return Encoding.ASCII.GetString(bytes, offset, 4);
        }


        [Test]
        public void FromSamples_WritesValidPcmHeader()
        {
            var bytes = WavUtility.FromSamples(new float[6], 2, 44100); // 3 stereo frames

            Assert.AreEqual(WavUtility.HeaderSize + 12, bytes.Length);
            Assert.AreEqual("RIFF", Ascii(bytes, 0));
            Assert.AreEqual(bytes.Length - 8, BitConverter.ToInt32(bytes, 4));
            Assert.AreEqual("WAVE", Ascii(bytes, 8));
            Assert.AreEqual("fmt ", Ascii(bytes, 12));
            Assert.AreEqual(16, BitConverter.ToInt32(bytes, 16));
            Assert.AreEqual(1, BitConverter.ToInt16(bytes, 20)); // PCM
            Assert.AreEqual(2, BitConverter.ToInt16(bytes, 22)); // Channels
            Assert.AreEqual(44100, BitConverter.ToInt32(bytes, 24)); // Sample rate
            Assert.AreEqual(44100 * 2 * 2, BitConverter.ToInt32(bytes, 28)); // Byte rate
            Assert.AreEqual(4, BitConverter.ToInt16(bytes, 32)); // Block align
            Assert.AreEqual(16, BitConverter.ToInt16(bytes, 34)); // Bits per sample
            Assert.AreEqual("data", Ascii(bytes, 36));
            Assert.AreEqual(12, BitConverter.ToInt32(bytes, 40));
        }


        [Test]
        public void FromSamples_WritesSamplesAfterHeader()
        {
            var bytes = WavUtility.FromSamples(new[] {0f, 1f, -1f, 0.5f}, 1, 8000);

            Assert.AreEqual(0, BitConverter.ToInt16(bytes, 44));
            Assert.AreEqual(32767, BitConverter.ToInt16(bytes, 46));
            Assert.AreEqual(-32767, BitConverter.ToInt16(bytes, 48));
            Assert.AreEqual(16384, BitConverter.ToInt16(bytes, 50));
        }


        [Test]
        public void FromSamples_ClipsOutOfRangeValues()
        {
            var bytes = WavUtility.FromSamples(new[] {2f, -2f, float.NaN}, 1, 8000);

            Assert.AreEqual(32767, BitConverter.ToInt16(bytes, 44));
            Assert.AreEqual(-32767, BitConverter.ToInt16(bytes, 46));
            Assert.AreEqual(0, BitConverter.ToInt16(bytes, 48));
        }


        [Test]
        public void FromSamples_Empty_WritesHeaderOnly()
        {
            var bytes = WavUtility.FromSamples(new float[0], 1, 8000);

            Assert.AreEqual(WavUtility.HeaderSize, bytes.Length);
            Assert.AreEqual(0, BitConverter.ToInt32(bytes, 40));
        }


        [Test]
        public void FromSamples_InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => WavUtility.FromSamples(null, 1, 8000));
            Assert.Throws<ArgumentOutOfRangeException>(() => WavUtility.FromSamples(new float[1], 0, 8000));
            Assert.Throws<ArgumentOutOfRangeException>(() => WavUtility.FromSamples(new float[1], 1, 0));
        }
    }
}
