using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace SOSXR.WhatUsesThis
{
    /// <summary>
    /// Reverse-dependency lookup for project assets. Maps each asset to the assets that use it, and caches the map in Temp/WhatUsesThis.json.
    /// Backs the What uses this? menu command, which rebuilds the cache on demand.
    /// </summary>
    public static class WhatUsesThis
    {
        private static Dictionary<string, List<string>> Dict => _dict ?? Load() ?? CleanBuild();
        private const string CacheFilename = "Temp/WhatUsesThis.json";

        private static Dictionary<string, List<string>> _dict;


        [MenuItem("SOSXR/Asset Dependency/WhatUsesThis Rebuild")]
        private static Dictionary<string, List<string>> CleanBuild()
        {
            try
            {
                EditorUtility.DisplayProgressBar("WhatUsesThis", "Getting Assets", 0.2f);

                var allAssets = AssetDatabase.FindAssets("").Select(AssetDatabase.GUIDToAssetPath).Distinct().ToArray();

                var dependencies = new Dictionary<string, string[]>();

                var i = 0;

                foreach (var asset in allAssets)
                {
                    dependencies[asset] = AssetDatabase.GetDependencies(asset, false);
                    i++;

                    if (i % 100 == 0 && EditorUtility.DisplayCancelableProgressBar("WhatUsesThis", $"Getting Dependencies [{i}/{allAssets.Length}]", i / (float) allAssets.Length))
                    {
                        // Null, not an empty dictionary, so callers can tell a cancel from "nothing uses this"
                        Debug.Log("WhatUsesThis: rebuild cancelled.");

                        return null;
                    }
                }

                EditorUtility.DisplayProgressBar("WhatUsesThis", "Building Dependents", 0.9f);

                AddDependencyToDictionary(dependencies);

                Save();

                return _dict;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }


        private static void AddDependencyToDictionary(Dictionary<string, string[]> dependencies)
        {
            _dict = new Dictionary<string, List<string>>();

            foreach (var dependency in dependencies)
            {
                foreach (var dependent in dependency.Value)
                {
                    if (!_dict.TryGetValue(dependent, out var list))
                    {
                        list = new List<string>();
                        _dict[dependent] = list;
                    }

                    list.Add(dependency.Key);
                }
            }
        }


        private static void Save()
        {
            if (_dict == null)
            {
                return;
            }

            using var stream = new FileStream(CacheFilename, FileMode.Create);

            JsonSerializer.Serialize(stream, _dict);
        }


        /// <summary>Drops the in-memory and on-disk cache so the next query rebuilds it.</summary>
        internal static void Invalidate()
        {
            _dict = null;

            try
            {
                if (File.Exists(CacheFilename))
                {
                    File.Delete(CacheFilename);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"WhatUsesThis: could not delete cache file {CacheFilename}: {e.Message}");
            }
        }


        private static Dictionary<string, List<string>> Load()
        {
            try
            {
                using var stream = new FileStream(CacheFilename, FileMode.Open);

                _dict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(stream);
            }
            catch (Exception)
            {
                _dict = null;
            }

            return _dict;
        }


        [MenuItem("SOSXR/Asset Dependency/What uses this? %#&d")]
        private static void FindParentAssets()
        {
            var iCount = 0;
            var dict = Dict;

            if (dict == null)
            {
                Debug.Log("<color=#5C93B9>What uses this? Search aborted: dependency cache was not built.</color>");

                return;
            }

            foreach (var selectedObj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                var selected = AssetDatabase.GetAssetPath(selectedObj);

                Debug.Log($"<color=#5C93B9>What uses <b>{selected}</b>?</color>", selectedObj);

                if (dict.TryGetValue(selected, out var dependants))
                {
                    foreach (var d in dependants)
                    {
                        Debug.Log($"<color=#8CA166>  {d}</color>", AssetDatabase.LoadAssetAtPath<Object>(d));
                        iCount++;
                    }
                }
            }

            Debug.Log($"<color=#5C93B9>Search complete, found <b>{iCount}</b> result{(iCount == 1 ? "" : "s")}</color>");
        }


        [MenuItem("SOSXR/Asset Dependency/What does this use? %&d")]
        private static void FindChildAssets()
        {
            var iCount = 0;

            foreach (var selectedObj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                var selected = AssetDatabase.GetAssetPath(selectedObj);

                Debug.Log($"<color=#5C93B9>What does <b>{selected}</b> use?</color>", selectedObj);

                foreach (var d in AssetDatabase.GetDependencies(selected, false))
                {
                    Debug.Log($"<color=#8CA166>  {d}</color>", AssetDatabase.LoadAssetAtPath<Object>(d));
                    iCount++;
                }
            }

            Debug.Log($"<color=#5C93B9>Search complete, found <b>{iCount}</b> result{(iCount == 1 ? "" : "s")}</color>");
        }
    }


    /// <summary>
    /// Invalidates the WhatUsesThis reverse-dependency cache whenever assets are imported, deleted or moved,
    /// so queries never answer from a stale dependency map.
    /// </summary>
    internal class WhatUsesThisCacheInvalidator : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Length == 0 && deletedAssets.Length == 0 && movedAssets.Length == 0)
            {
                return;
            }

            WhatUsesThis.Invalidate();
        }
    }
}
