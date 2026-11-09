using UnityEditor;
using UnityEditor.Build;
using UnityEngine;


namespace SOSXR.EditorSpice.EditorScripts
{
    [InitializeOnLoad]
    public class DefineSymbolManager
    {
        static DefineSymbolManager()
        {
            AddDefineSymbol("SOSXR_EDITORTOOLS_INSTALLED");
        }


        public static void AddDefineSymbol(string defineSymbol)
        {
            var namedBuildTarget = GetCurrentNamedBuildTarget();

            var currentDefines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

            if (!DefineSymbolUtility.Contains(currentDefines, defineSymbol))
            {
                currentDefines = DefineSymbolUtility.Add(currentDefines, defineSymbol);
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, currentDefines);
                Debug.Log($"Added define symbol: {defineSymbol}");
            }
        }


        public static void RemoveDefineSymbol(string defineSymbol)
        {
            var namedBuildTarget = GetCurrentNamedBuildTarget();

            var currentDefines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

            if (DefineSymbolUtility.Contains(currentDefines, defineSymbol))
            {
                currentDefines = DefineSymbolUtility.Remove(currentDefines, defineSymbol); // Whole-symbol removal; other symbols stay intact
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, currentDefines);
                Debug.Log($"Removed define symbol: {defineSymbol}");
            }
            else
            {
                Debug.LogWarning($"Define symbol {defineSymbol} not found.");
            }
        }


        private static NamedBuildTarget GetCurrentNamedBuildTarget()
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            return NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        }
    }


    // This class is used to handle asset changes, such as when a script or package is removed
    public class AssetPostprocessorHandler : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var deletedAsset in deletedAssets)
            {
                // Debug.LogFormat("Deleted asset: {0}", deletedAsset);

                if (!IsEditorSpiceAsset(deletedAsset))
                {
                    continue;
                }

                // If the package or script is deleted, remove the define symbol
                DefineSymbolManager.RemoveDefineSymbol("SOSXR_EDITORTOOLS_INSTALLED");

                Debug.Log("SOSXR Editor Tools package removed. Define symbol removed.");

                break;
            }
        }


        /// <summary>
        ///     True when the deleted path is the EditorSpice package root folder or its package.json.
        ///     Deleting a single file inside the package does not count.
        /// </summary>
        public static bool IsEditorSpiceAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            var path = assetPath.Replace('\\', '/').TrimEnd('/');

            if (path.EndsWith("/package.json", System.StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - "/package.json".Length);
            }

            var folder = path.Substring(path.LastIndexOf('/') + 1);

            return folder.Equals("sosxr-unity-editorspice", System.StringComparison.OrdinalIgnoreCase)
                   || folder.Equals("com.sosxr.editorspice", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}