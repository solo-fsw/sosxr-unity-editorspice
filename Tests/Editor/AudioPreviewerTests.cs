using NUnit.Framework;
using SOSXR.EditorSpice.EditorScripts;
using UnityEngine;


namespace SOSXR.EditorSpice.Tests
{
    public class AudioPreviewerTests
    {
        private AudioClip _clipA;
        private AudioClip _clipB;


        [SetUp]
        public void SetUp()
        {
            _clipA = AudioClip.Create("A", 441, 1, 44100, false);
            _clipB = AudioClip.Create("B", 441, 1, 44100, false);
        }


        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_clipA);
            Object.DestroyImmediate(_clipB);
        }


        private static Event Key(KeyCode keyCode, EventModifiers modifiers = EventModifiers.None, EventType type = EventType.KeyDown)
        {
            return new Event { type = type, keyCode = keyCode, modifiers = modifiers };
        }


        [Test]
        public void ShouldPlay_NothingPlaying_Plays()
        {
            Assert.IsTrue(AudioPreviewer.ShouldPlay(false, null, _clipA));
            Assert.IsTrue(AudioPreviewer.ShouldPlay(false, _clipA, _clipA));
        }


        [Test]
        public void ShouldPlay_SameClipPlaying_StopsOnly()
        {
            Assert.IsFalse(AudioPreviewer.ShouldPlay(true, _clipA, _clipA));
        }


        [Test]
        public void ShouldPlay_OtherClipPlaying_Switches()
        {
            Assert.IsTrue(AudioPreviewer.ShouldPlay(true, _clipA, _clipB));
            Assert.IsTrue(AudioPreviewer.ShouldPlay(true, null, _clipB));
        }


        [Test]
        public void IsSpacebarPreviewEvent_PlainSpaceInProjectWindow_True()
        {
            Assert.IsTrue(AudioPreviewer.IsSpacebarPreviewEvent(Key(KeyCode.Space), false, true));
        }


        [Test]
        public void IsSpacebarPreviewEvent_FunctionKeyModifier_StillTrue()
        {
            Assert.IsTrue(AudioPreviewer.IsSpacebarPreviewEvent(Key(KeyCode.Space, EventModifiers.FunctionKey), false, true));
        }


        [Test]
        public void IsSpacebarPreviewEvent_ProjectWindowNotFocused_False()
        {
            Assert.IsFalse(AudioPreviewer.IsSpacebarPreviewEvent(Key(KeyCode.Space), false, false));
        }


        [Test]
        public void IsSpacebarPreviewEvent_EditingTextField_False()
        {
            Assert.IsFalse(AudioPreviewer.IsSpacebarPreviewEvent(Key(KeyCode.Space), true, true));
        }


        [TestCase(EventModifiers.Shift)]
        [TestCase(EventModifiers.Control)]
        [TestCase(EventModifiers.Alt)]
        [TestCase(EventModifiers.Command)]
        public void IsSpacebarPreviewEvent_BlockingModifier_False(EventModifiers modifiers)
        {
            Assert.IsFalse(AudioPreviewer.IsSpacebarPreviewEvent(Key(KeyCode.Space, modifiers), false, true));
        }


        [Test]
        public void IsSpacebarPreviewEvent_OtherKeyOrEventType_False()
        {
            Assert.IsFalse(AudioPreviewer.IsSpacebarPreviewEvent(Key(KeyCode.Return), false, true));
            Assert.IsFalse(AudioPreviewer.IsSpacebarPreviewEvent(Key(KeyCode.Space, type: EventType.KeyUp), false, true));
            Assert.IsFalse(AudioPreviewer.IsSpacebarPreviewEvent(null, false, true));
        }
    }
}
