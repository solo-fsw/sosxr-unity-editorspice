using NUnit.Framework;
using SOSXR.EditorSpice.EditorScripts;
using UnityEngine;


namespace SOSXR.EditorSpice.Tests
{
    public class AudioEditorTests
    {
        private const int Frequency = 1000;
        private AudioClip _clip;
        private AudioEditor _window;


        [SetUp]
        public void SetUp()
        {
            _clip = AudioClip.Create("TestClip", Frequency * 2, 2, Frequency, false); // 2 seconds, stereo
            var samples = new float[_clip.samples * _clip.channels];

            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = 1f;
            }

            _clip.SetData(samples, 0);
            _window = ScriptableObject.CreateInstance<AudioEditor>();
        }


        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_window);
            Object.DestroyImmediate(_clip);
        }


        [Test]
        public void SetClip_LoadsSamplesAndSelectsWholeClip()
        {
            _window.SetClip(_clip);

            Assert.IsTrue(_window.HasSamples, _window.LoadError);
            Assert.AreEqual(0f, _window.StartTrim);
            Assert.AreEqual(2f, _window.EndTrim, 1e-4f);
            Assert.AreEqual(Frequency * 2 * 2, _window.GetProcessedSamples().Length);
        }


        [Test]
        public void SetEdit_TrimsAndFadesProcessedSamples()
        {
            _window.SetClip(_clip);
            _window.SetEdit(0.5f, 1.5f, 0.1f, 0.1f);

            var samples = _window.GetProcessedSamples();

            Assert.AreEqual(Frequency * 2, samples.Length); // 1 second, stereo
            Assert.AreEqual(0f, samples[0], 1e-5f);
            Assert.AreEqual(1f, samples[samples.Length / 2], 1e-5f);
            Assert.AreEqual(0f, samples[samples.Length - 1], 1e-5f);
        }


        [Test]
        public void SetEdit_ClampsOutOfRangeValues()
        {
            _window.SetClip(_clip);
            _window.SetEdit(-1f, 10f, 5f, 5f);

            Assert.AreEqual(0f, _window.StartTrim);
            Assert.AreEqual(2f, _window.EndTrim, 1e-4f);
            Assert.AreEqual(2f, _window.FadeIn, 1e-4f);
            Assert.AreEqual(0f, _window.FadeOut, 1e-4f);
        }


        [Test]
        public void SetClip_Null_ClearsSamples()
        {
            _window.SetClip(_clip);
            _window.SetClip(null);

            Assert.IsFalse(_window.HasSamples);
            CollectionAssert.IsEmpty(_window.GetProcessedSamples());
        }


        [TestCase("Assets/Audio/Clip.wav", "Assets/Audio")]
        [TestCase("Assets/Clip.wav", "Assets")]
        [TestCase("Packages/com.some.package/Clip.wav", "Assets")]
        [TestCase(@"Assets\Audio\Clip.wav", "Assets/Audio")]
        [TestCase("", "Assets")]
        public void GetSaveFolder_NextToSourceOrAssetsRoot(string sourcePath, string expected)
        {
            Assert.AreEqual(expected, AudioEditor.GetSaveFolder(sourcePath));
        }
    }
}
