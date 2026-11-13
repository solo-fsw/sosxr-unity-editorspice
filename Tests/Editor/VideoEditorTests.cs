using System.Text.RegularExpressions;
using NUnit.Framework;
using SOSXR.EditorSpice.EditorScripts;
using UnityEngine;
using UnityEngine.TestTools;


namespace SOSXR.EditorSpice.Tests
{
    public class VideoEditorTests
    {
        private VideoEditor _window;


        [SetUp]
        public void SetUp()
        {
            _window = ScriptableObject.CreateInstance<VideoEditor>();
        }


        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_window);
        }


        [Test]
        public void NoClip_TrimIsEmptyAndNoPlayer()
        {
            Assert.IsNull(_window.Clip);
            Assert.AreEqual(0f, _window.StartTrim);
            Assert.AreEqual(0f, _window.EndTrim);
            Assert.IsNull(_window.Player);
        }


        [Test]
        public void Play_WithoutClip_WarnsAndReturnsFalse()
        {
            LogAssert.Expect(LogType.Warning, new Regex("no clip assigned"));

            Assert.IsFalse(_window.Play());
        }


        [Test]
        public void Record_OutsidePlayModeOrWithoutRecorder_DoesNotRecord()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Recorder|Play Mode"));

            Assert.IsFalse(_window.Record());
            Assert.IsFalse(VideoTrimRecorder.IsRecording);
        }
    }


    public class VideoTrimPlayerTests
    {
        private VideoTrimPlayer _player;


        [SetUp]
        public void SetUp()
        {
            _player = new VideoTrimPlayer();
        }


        [TearDown]
        public void TearDown()
        {
            _player.Dispose();
        }


        [Test]
        public void NewPlayer_IsIdle()
        {
            Assert.IsFalse(_player.IsPlaying);
            Assert.IsFalse(_player.IsPaused);
            Assert.IsNull(_player.RenderTexture);
            Assert.AreEqual(0d, _player.CurrentTime);
        }


        [Test]
        public void Play_WithoutClip_WarnsAndReturnsFalse()
        {
            LogAssert.Expect(LogType.Warning, new Regex("no clip assigned"));

            Assert.IsFalse(_player.Play(null, 0d, 1d));
            Assert.IsFalse(_player.IsPlaying);
        }


        [Test]
        public void PauseResumeStop_WhenIdle_ReturnFalse()
        {
            Assert.IsFalse(_player.Pause());
            Assert.IsFalse(_player.Resume());
            Assert.IsFalse(_player.Stop());
        }


        [Test]
        public void TickAndDispose_WhenIdle_DoNotThrow()
        {
            Assert.DoesNotThrow(() => _player.Tick());
            Assert.DoesNotThrow(() => _player.Dispose());
            Assert.DoesNotThrow(() => _player.Dispose());
        }


        [Test]
        public void Record_WithoutClip_DoesNotRecord()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Recorder|Play Mode|VideoClip"));

            Assert.IsFalse(VideoTrimRecorder.Record(_player, null, 0d, 1d));
            Assert.IsFalse(VideoTrimRecorder.IsRecording);
        }
    }
}
