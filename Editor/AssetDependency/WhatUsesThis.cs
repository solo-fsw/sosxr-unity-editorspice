using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace SOSXR.WhatUsesThis
{
    public static class WhatUsesThis
    {
        private static Dictionary<string, List<string>> Dict => _dict ?? Load() ?? CleanBuild();
        private const string CacheFilename = "Temp/WhatUsesThis.bin";

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
                        return new Dictionary<string, List<string>>();
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

            var bin = new BinaryFormatter();
            bin.Serialize(stream, _dict);
        }


        private static Dictionary<string, List<string>> Load()
        {
            try
            {
                using var stream = new FileStream(CacheFilename, FileMode.Open);

                var bin = new BinaryFormatter();
                _dict = (Dictionary<string, List<string>>) bin.Deserialize(stream);
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

            foreach (var selectedObj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                var selected = AssetDatabase.GetAssetPath(selectedObj);

                Debug.Log($"<color=#5C93B9>What uses <b>{selected}</b>?</color>", selectedObj);

                if (Dict.TryGetValue(selected, out var dependants))
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
}