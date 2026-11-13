using System.Text.RegularExpressions;
using NUnit.Framework;
using SOSXR.BuildHelpers;
using SOSXR.EditorSpice.EditorScripts;
using Tarodev;
using UnityEditor;
using UnityEditor.Build.Reporting;
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


    public class BuildVersionSequenceTests
    {
        private BuildInfoDetails _details;


        [SetUp]
        public void SetUp()
        {
            _details = ScriptableObject.CreateInstance<BuildInfoDetails>();
        }


        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_details);
        }


        [Test]
        public void SeedLastSuccessfulVersion_PreviousBuildSucceeded_UsesCurrentVersion()
        {
            _details.LastBuildResult = BuildResult.Succeeded;

            BuildInfoManager.SeedLastSuccessfulVersion(_details, "0_0_64d", 64);

            Assert.AreEqual("0_0_64d", _details.LastSuccessfulSemVer);
            Assert.AreEqual(64, _details.LastSuccessfulBundleVersionCode);
        }


        [TestCase(BuildResult.Unknown)]
        [TestCase(BuildResult.Failed)]
        [TestCase(BuildResult.Cancelled)]
        public void SeedLastSuccessfulVersion_PreviousBuildNotSucceeded_UsesOneBelowCurrentVersion(BuildResult lastResult)
        {
            // The current version was set for a build that did not succeed
            _details.LastBuildResult = lastResult;

            BuildInfoManager.SeedLastSuccessfulVersion(_details, "0_0_64d", 64);

            Assert.AreEqual("0_0_63d", _details.LastSuccessfulSemVer);
            Assert.AreEqual(63, _details.LastSuccessfulBundleVersionCode);
        }


        [Test]
        public void SeedLastSuccessfulVersion_AlreadyKnown_DoesNotOverwrite()
        {
            _details.LastSuccessfulSemVer = "0_0_10d";
            _details.LastSuccessfulBundleVersionCode = 10;
            _details.LastBuildResult = BuildResult.Failed;

            BuildInfoManager.SeedLastSuccessfulVersion(_details, "0_0_64d", 64);

            Assert.AreEqual("0_0_10d", _details.LastSuccessfulSemVer);
            Assert.AreEqual(10, _details.LastSuccessfulBundleVersionCode);
        }


        [Test]
        public void GetNextVersion_IsOneAboveLastSuccessfulBuild()
        {
            _details.LastSuccessfulSemVer = "0_0_64d";
            _details.LastSuccessfulBundleVersionCode = 64;

            var (semVer, bundleVersionCode) = BuildInfoManager.GetNextVersion(_details, true);

            Assert.AreEqual("0_0_65d", semVer);
            Assert.AreEqual(65, bundleVersionCode);
        }


        [Test]
        public void GetNextVersion_SwitchToProduction_KeepsNumberChangesIndicator()
        {
            _details.LastSuccessfulSemVer = "0_0_64d";
            _details.LastSuccessfulBundleVersionCode = 64;

            var (semVer, _) = BuildInfoManager.GetNextVersion(_details, false);

            Assert.AreEqual("0_0_65p", semVer);
        }


        [Test]
        public void Sequence_FailedAndUnfinishedBuilds_DoNotConsumeVersionNumbers()
        {
            _details.LastSuccessfulSemVer = "0_0_5d";
            _details.LastSuccessfulBundleVersionCode = 5;

            // Success
            ApplyNextVersion();
            Assert.AreEqual("0_0_6d", _details.SemVer);
            Assert.AreEqual(6, _details.AndroidBundleVersionCode);
            BuildInfoManager.RecordSuccessfulBuild(_details);

            // Failed build: nothing recorded
            ApplyNextVersion();
            Assert.AreEqual("0_0_7d", _details.SemVer);

            // Build that never reached post-processing: nothing recorded
            ApplyNextVersion();
            Assert.AreEqual("0_0_7d", _details.SemVer);

            // Success reuses the number the failed builds tried
            ApplyNextVersion();
            Assert.AreEqual("0_0_7d", _details.SemVer);
            Assert.AreEqual(7, _details.AndroidBundleVersionCode);
            BuildInfoManager.RecordSuccessfulBuild(_details);

            // Next success is one higher than the last success
            ApplyNextVersion();
            Assert.AreEqual("0_0_8d", _details.SemVer);
            Assert.AreEqual(8, _details.AndroidBundleVersionCode);
        }


        private void ApplyNextVersion()
        {
            (_details.SemVer, _details.AndroidBundleVersionCode) = BuildInfoManager.GetNextVersion(_details, true);
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


        [Test]
        public void FindFrameAndRenameHook_ExistsInThisUnityVersion()
        {
            // Regression: rename after drop used GetWindow(SceneHierarchyWindow), which opened a legacy Hierarchy
            // in Unity 6.6. The fix relies on this internal hook; fails here first if Unity renames it.
            var hook = HierarchyScriptDropHandler.FindFrameAndRenameHook();
            Assert.IsNotNull(hook);
            Assert.AreEqual(0, hook.GetParameters().Length);
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
