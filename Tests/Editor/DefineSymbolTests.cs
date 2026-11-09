using NUnit.Framework;
using SOSXR.EditorSpice.EditorScripts;


namespace SOSXR.EditorSpice.Tests
{
    public class DefineSymbolTests
    {
        [Test]
        public void Add_KeepsExistingSymbols()
        {
            // Regression: SetScriptingDefineSymbols used to replace the whole list
            Assert.AreEqual("FOO;BAR;EDITORSPICE", DefineSymbolUtility.Add("FOO;BAR", "EDITORSPICE"));
        }


        [Test]
        public void Add_IsIdempotent()
        {
            Assert.AreEqual("FOO;BAR", DefineSymbolUtility.Add("FOO;BAR", "FOO"));
        }


        [TestCase(null)]
        [TestCase("")]
        public void Add_ToEmptyList_ReturnsOnlySymbol(string defines)
        {
            Assert.AreEqual("EDITORSPICE", DefineSymbolUtility.Add(defines, "EDITORSPICE"));
        }


        [Test]
        public void Contains_MatchesWholeSymbolOnly()
        {
            // Regression: substring Contains treated EDITORSPICE_OLD as EDITORSPICE
            Assert.IsFalse(DefineSymbolUtility.Contains("EDITORSPICE_OLD;FOO", "EDITORSPICE"));
            Assert.IsTrue(DefineSymbolUtility.Contains("FOO; EDITORSPICE ;BAR", "EDITORSPICE"));
        }


        [Test]
        public void Remove_LeavesOtherSymbolsIntact()
        {
            // Regression: string.Replace mangled symbols that contained the removed one
            Assert.AreEqual("FOO_BAR;BAZ", DefineSymbolUtility.Remove("FOO;FOO_BAR;BAZ", "FOO"));
        }


        [Test]
        public void Remove_MissingSymbol_ReturnsNormalisedList()
        {
            Assert.AreEqual("A;B", DefineSymbolUtility.Remove("A;;B;", "C"));
        }


        [TestCase("Assets/_SOSXR/sosxr-unity-editorspice", true)]
        [TestCase("Assets/_SOSXR/sosxr-unity-editorspice/package.json", true)]
        [TestCase("Packages/com.sosxr.editorspice", true)]
        [TestCase("Packages/com.sosxr.editorspice/package.json", true)]
        [TestCase("Assets/_SOSXR/sosxr-unity-editorspice/Editor/MenuItems.cs", false)]
        [TestCase("Assets/SomethingElse/package.json", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsEditorSpiceAsset_MatchesPackageRootOnly(string path, bool expected)
        {
            // Regression: the old check looked for "SOSXR_EditorSpice", which never matched
            Assert.AreEqual(expected, AssetPostprocessorHandler.IsEditorSpiceAsset(path));
        }
    }
}
