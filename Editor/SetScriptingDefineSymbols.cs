using UnityEditor;
using UnityEditor.Build;


namespace SOSXR.EditorSpice.EditorScripts
{
    [InitializeOnLoad] // This will call the constructor of the class when Unity starts
    public class SetScriptingDefineSymbols
    {
        static SetScriptingDefineSymbols()
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(
                EditorUserBuildSettings.activeBuildTarget
            );
            var currentBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var currentDefines = PlayerSettings.GetScriptingDefineSymbols(currentBuildTarget);

            if (DefineSymbolUtility.Contains(currentDefines, _defineSymbol))
            {
                return;
            }

            // Append to the existing list. Never replace it: that would delete the project's other defines.
            PlayerSettings.SetScriptingDefineSymbols(
                currentBuildTarget,
                DefineSymbolUtility.Add(currentDefines, _defineSymbol)
            );
        }


        private const string _defineSymbol = "EDITORSPICE";
    }
}
