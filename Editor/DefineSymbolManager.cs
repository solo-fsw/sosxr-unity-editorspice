using UnityEditor;
using UnityEditor.Build;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    [InitializeOnLoad]
    public class DefineSymbolManager
    {
        static DefineSymbolManager()
        {
            // Add the define symbol when the script is loaded
            AddDefineSymbol("SOSXR_EDITORTOOLS_INSTALLED");
        }


        // Add the define symbol to the current build target
        public static void AddDefineSymbol(string defineSymbol)
        {
            var namedBuildTarget = GetCurrentNamedBuildTarget();

            var currentDefines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

            if (!currentDefines.Contains(defineSymbol))
            {
                currentDefines = currentDefines + ";" + defineSymbol;
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, currentDefines);
                Debug.Log($"Added define symbol: {defineSymbol}");
            }
        }


        // Remove the define symbol from the current build target
        public static void RemoveDefineSymbol(string defineSymbol)
        {
            var namedBuildTarget = GetCurrentNamedBuildTarget();

            var currentDefines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

            if (currentDefines.Contains(defineSymbol))
            {
                currentDefines = currentDefines.Replace(defineSymbol, "").Replace(";;", ";"); // Remove the symbol and clean up extra semicolons
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, currentDefines);
                Debug.Log($"Removed define symbol: {defineSymbol}");
            }
            else
            {
                Debug.LogWarning($"Define symbol {defineSymbol} not found.");
            }
        }


        // Helper method to get the current NamedBuildTarget
        private static NamedBuildTarget GetCurrentNamedBuildTarget()
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            return NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        }
    }


    // This class is used to handle asset changes, such as when a script or package is removed
    public class AssetPostprocessorHandler : AssetPostprocessor
    {
        // Called when assets are imported, deleted, or moved
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Check if any assets related to the package (or script) have been deleted
            foreach (var deletedAsset in deletedAssets)
            {
                // Debug.LogFormat("Deleted asset: {0}", deletedAsset);

                if (!deletedAsset.Contains("SOSXR_EditorSpice"))
                {
                    continue;
                }

                // If the package or script is deleted, remove the define symbol
                DefineSymbolManager.RemoveDefineSymbol("SOSXR_EDITORTOOLS_INSTALLED");

                Debug.Log("SOSXR Editor Tools package removed. Define symbol removed.");

                break;
            }
        }
    }
}