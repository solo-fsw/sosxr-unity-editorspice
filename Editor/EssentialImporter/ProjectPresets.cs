using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    /// Applies SOSXR preset assets to the project's PlayerSettings and QualitySettings, and registers default presets per asset type.
    /// Presets are found by name under Assets/_SOSXR and Packages.
    /// </summary>
    public static class ProjectPresets
    {
        private static string[] _foldersToSearch
        {
            get { return new[] {"Assets/_SOSXR", "Packages"}; }
        }

        private const string InitialPresetsAppliedKey = "SOSXR.InitialPresetsApplied";

        private static bool _initialPresetsApplied // Stored in EditorUserSettings (UserSettings/), so it persists between sessions but is scoped to this project
        {
            get => EditorUserSettings.GetConfigValue(InitialPresetsAppliedKey) == "true";
            set => EditorUserSettings.SetConfigValue(InitialPresetsAppliedKey, value ? "true" : "false");
        }

        private static readonly string _defaultFilter = "t:preset SOSXR ";
        private static readonly List<string> _paths = new();


        /// <summary>
        ///     These are presets that in theory should be applied to projects only once, at the beginning.
        ///     A dialog will display if the presets have already been applied, asking for confirmation.
        /// </summary>
        [MenuItem("SOSXR/Setup/Setup Initial Presets")]
        private static async void SetInitialPresets()
        {
            try
            {
                if (_initialPresetsApplied)
                {
                    var confirm = EditorUtility.DisplayDialog("Initial Presets Already Applied", "Initial presets have already been applied. Are you sure you want to apply them again?", "Yes", "No");

                    if (!confirm)
                    {
                        return;
                    }
                }

                // Static list: drop paths collected by an earlier run so presets are not imported twice
                _paths.Clear();

                await SetPlayerSettingsPreset();
                await SetQualitySettingsPreset();

                foreach (var path in _paths)
                {
                    await Import(path);
                    Debug.Log($"Successfully applied preset to {path}");
                }

                AssetDatabase.Refresh();
                _initialPresetsApplied = true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }


        /// <summary>
        ///     PlayerSettings properties that identify a specific project. These are never taken from the preset,
        ///     so applying it to an existing project keeps its bundle IDs, versions, name and GUID.
        /// </summary>
        private static readonly string[] _playerSettingsIdentityPrefixes =
        {
            "productGUID", "clonedFromGUID", "productName", "companyName", "bundleVersion",
            "applicationIdentifier", "buildNumber", "AndroidBundleVersionCode", "metroPackageName",
            "metroPackageVersion", "XboxOneProductId", "XboxOneVersion", "XboxOneAllowedProductIds"
        };


        private static bool IsIdentityProperty(string propertyPath)
        {
            return _playerSettingsIdentityPrefixes.Any(prefix => propertyPath == prefix || propertyPath.StartsWith(prefix + ".", StringComparison.Ordinal));
        }


        private static async Task SetPlayerSettingsPreset()
        {
            var targets = Resources.FindObjectsOfTypeAll<PlayerSettings>();

            var filter = "PlayerSettings";
            var preset = GetPreset(filter);

            foreach (var target in targets) // For some reason there are multiple PlayerSettings instances, but not all are the correct ones. 
            {
                if (target == null)
                {
                    continue;
                }

                if (preset == null)
                {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(target);

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var nonIdentityPaths = preset.PropertyModifications
                                             .Select(modification => modification.propertyPath)
                                             .Where(propertyPath => !IsIdentityProperty(propertyPath))
                                             .Distinct()
                                             .ToArray();

                if (preset.ApplyTo(target, nonIdentityPaths))
                {
                    _paths.Add(path);
                }
                else
                {
                    Debug.LogError($"Failed to apply preset to {filter}");
                }
            }

            await Task.CompletedTask;
        }


        private static QualitySettings GetActiveQualitySettings()
        {
            return Resources.FindObjectsOfTypeAll<QualitySettings>().FirstOrDefault(qs => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(qs)));
        }


        private static async Task SetQualitySettingsPreset()
        {
            var target = GetActiveQualitySettings();

            if (target == null)
            {
                Debug.LogError("No active QualitySettings instance found.");

                return;
            }

            var filter = "Quality";
            var preset = GetPreset(filter);

            if (preset == null)
            {
                Debug.LogError($"Preset not found for {filter}");

                return;
            }

            if (preset.ApplyTo(target))
            {
                var path = AssetDatabase.GetAssetPath(target);

                if (!string.IsNullOrEmpty(path))
                {
                    _paths.Add(path);
                }
            }
            else
            {
                Debug.LogError($"Failed to apply preset to {filter}");
            }

            await Task.CompletedTask;
        }


        private static Preset GetPreset(string filter)
        {
            var fullFilter = _defaultFilter + filter;
            var guids = AssetDatabase.FindAssets(fullFilter, _foldersToSearch);

            if (guids.Length == 0)
            {
                Debug.LogWarning("No presets found with filter " + fullFilter);

                return null;
            }

            if (guids.Length > 1)
            {
                Debug.LogWarning($"Multiple presets found using filter {fullFilter}. Using the first one.");
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var preset = AssetDatabase.LoadAssetAtPath<Preset>(path);

            if (preset == null)
            {
                Debug.LogError($"Preset not found for GUID: {guids[0]}");

                return null;
            }

            return preset;
        }


        private static async Task Import(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Skipping import for empty or null path.");

                return;
            }

            AssetDatabase.ImportAsset(path);
            await Task.CompletedTask;
        }


        /// <summary>
        ///     Based on Warped Imagination: https://www.youtube.com/watch?v=KFmP1Q8NySod
        /// </summary>
        [MenuItem("SOSXR/Setup/Setup Default Presets")]
        private static void SetupDefaultPresetsMenuOption()
        {
            var filter = "Default";
            var fullFilter = _defaultFilter + filter;
            var guids = AssetDatabase.FindAssets(fullFilter, _foldersToSearch);

            if (guids.Length == 0)
            {
                Debug.LogWarning("No presets found with filter " + fullFilter);

                return;
            }

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                var preset = AssetDatabase.LoadAssetAtPath<Preset>(path);

                if (preset == null)
                {
                    continue;
                }

                var type = preset.GetPresetType();

                var list = new List<DefaultPreset>(Preset.GetDefaultPresetsForType(type));

                // Already registered: skip this preset only, keep processing the rest
                if (list.Any(defaultPreset => defaultPreset.preset == preset))
                {
                    continue;
                }

                var presetFilter = preset.name.Replace("_SOSXR_Default", "").Trim().Split('_').Last();

                // This means that the preset nane should end in _SOSXR_Default, and that before that should be the name of the filter I want to apply
                // This is a clunky way, but it works for now. I'll improve it later.
                // So the preset name TextureImporter_icon_SOSXR_Default will apply to TextureImporter, and the filter will be icon
                // This means that any texture with the word icon in the name will have this preset applied to it.
                // This should be improved since this is vastly error prone.

                list.Add(new DefaultPreset(presetFilter, preset));

                Preset.SetDefaultPresetsForType(type, list.ToArray());
            }
        }
    }
}