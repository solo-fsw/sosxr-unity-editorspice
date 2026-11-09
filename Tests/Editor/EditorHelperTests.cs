using System.Text.RegularExpressions;
using NUnit.Framework;
using SOSXR.BuildHelpers;
using SOSXR.EditorSpice.EditorScripts;
using Tarodev;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;


namespace SOSXR.EditorSpice.Tests
{
    public class BuildVersionTests
    {
        [TestCase("0_0_1p", true, false, "0_0_2p")]
        [TestCase("0_0_1p", true, true, "0_0_2d")]
        [TestCase("1_2_3d", false, true, "1_2_2d")]
        [TestCase("0_0_0p", false, false, "0_0_0p")]
        [TestCase("alpha-1_32_2p", true, false, "alpha-1_32_3p")]
        public void GenerateNextVersion_ChangesPatchNumber(string version, bool increment, bool development, string expected)
        {
            Assert.AreEqual(expected, BuildInfoManager.GenerateNextVersion(version, increment, development, "d", "p", "0_0_1"));
        }


        [Test]
        public void GenerateNextVersion_InvalidFormat_ResetsToInitial()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Invalid version format"));

            Assert.AreEqual("0_0_1p", BuildInfoManager.GenerateNextVersion("garbage", true, false, "d", "p", "0_0_1"));
        }
    }


    public class AutoSaveTests
    {
        [Test]
        public void CanAutoSave_OnlyWhenIdle()
        {
            Assert.IsTrue(TarodevAutoSave.CanAutoSave(true, false, false, false, true));
            Assert.IsFalse(TarodevAutoSave.CanAutoSave(false, false, false, false, true));
            Assert.IsFalse(TarodevAutoSave.CanAutoSave(true, true, false, false, true), "Must not save while entering or in play mode");
            Assert.IsFalse(TarodevAutoSave.CanAutoSave(true, false, true, false, true));
            Assert.IsFalse(TarodevAutoSave.CanAutoSave(true, false, false, true, true));
            Assert.IsFalse(TarodevAutoSave.CanAutoSave(true, false, false, false, false));
        }


        [TestCase("Assets/Scenes/Main.unity", true, true)]
        [TestCase("Assets/Scenes/Main.unity", false, false)]
        [TestCase("", true, false)]
        [TestCase(null, true, false)]
        public void CanSaveSceneSilently_SkipsUntitledScenes(string path, bool dirty, bool expected)
        {
            // Regression: SaveOpenScenes() popped a Save-As dialog for untitled scenes every interval
            Assert.AreEqual(expected, TarodevAutoSave.CanSaveSceneSilently(path, dirty));
        }
    }


    public class HierarchyDropTests
    {
        [Test]
        public void ShouldHandleDrop_DropOntoGameObject_LeavesItToUnity()
        {
            // Regression: the handler claimed every drop, so dropping onto a GameObject no longer added the component
            Assert.IsFalse(HierarchyScriptDropHandler.ShouldHandleDrop(true, HierarchyDropFlags.DropUpon));
        }


        [Test]
        public void ShouldHandleDrop_DropOnEmptySpace_CreatesGameObject()
        {
            Assert.IsTrue(HierarchyScriptDropHandler.ShouldHandleDrop(false, HierarchyDropFlags.None));
            Assert.IsTrue(HierarchyScriptDropHandler.ShouldHandleDrop(false, HierarchyDropFlags.DropUpon));
        }
    }


    public class ReadmeShowerTests
    {
        [Test]
        public void PickReadme_PrefersReadmeClosestToRoot()
        {
            var paths = new[]
            {
                "Assets/Plugins/Thing/README.md",
                "Assets/Docs/Readme.txt",
                "Assets/README.md.meta",
                "Assets/README.md",
                "Assets/A/README.md"
            };

            Assert.AreEqual("Assets/README.md", ReadmeShower.PickReadme(paths));
        }


        [Test]
        public void PickReadme_IsCaseInsensitiveAndStable()
        {
            var paths = new[] {"Assets/b/readme.md", "Assets/a/ReadMe.md"};

            Assert.AreEqual("Assets/a/ReadMe.md", ReadmeShower.PickReadme(paths));
        }


        [Test]
        public void PickReadme_NoMatch_ReturnsNull()
        {
            Assert.IsNull(ReadmeShower.PickReadme(new[] {"Assets/Notes.md"}));
            Assert.IsNull(ReadmeShower.PickReadme(null));
        }
    }
}
