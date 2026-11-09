using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


[InitializeOnLoad]
public class ReadmeShower : Editor
{
    private static readonly string kShowedReadmeSessionStateName = "ReadmeEditor.showedReadme";


    static ReadmeShower()
    {
        EditorApplication.delayCall += SelectReadmeAutomatically; // Show on Editor Start
    }


    private static void SelectReadmeAutomatically()
    {
        if (SessionState.GetBool(kShowedReadmeSessionStateName, false))
        {
            return;
        }

        SelectReadme();
        SessionState.SetBool(kShowedReadmeSessionStateName, true);
    }


    /// <summary>
    ///     Selects the project's README.md. Uses the AssetDatabase index instead of scanning the disk,
    ///     and picks the README closest to the Assets root, so the choice is stable.
    /// </summary>
    [MenuItem("SOSXR/Show README.md", priority = 100)]
    private static void SelectReadme()
    {
        var candidates = AssetDatabase
                         .FindAssets("README", new[] {"Assets"})
                         .Select(AssetDatabase.GUIDToAssetPath);

        var readmePath = PickReadme(candidates);

        if (readmePath == null)
        {
            Debug.Log("Couldn't find a readme file with .md extension");

            return;
        }

        var asset = AssetDatabase.LoadAssetAtPath<Object>(readmePath);

        if (asset != null)
        {
            Selection.activeObject = asset;
        }
        else
        {
            Debug.LogWarning("The .md file was not found as an asset in the AssetDatabase. It might not be imported as an asset.");
        }
    }


    /// <summary>
    ///     Returns the README.md path closest to the root (fewest folders), then shortest, then alphabetical.
    ///     Returns null when no path is named README.md (case-insensitive).
    /// </summary>
    public static string PickReadme(IEnumerable<string> assetPaths)
    {
        if (assetPaths == null)
        {
            return null;
        }

        return assetPaths
               .Where(p => !string.IsNullOrEmpty(p))
               .Select(p => p.Replace('\\', '/'))
               .Where(p => string.Equals(Path.GetFileName(p), "README.md", StringComparison.OrdinalIgnoreCase))
               .OrderBy(p => p.Count(c => c == '/'))
               .ThenBy(p => p.Length)
               .ThenBy(p => p, StringComparer.Ordinal)
               .FirstOrDefault();
    }
}
