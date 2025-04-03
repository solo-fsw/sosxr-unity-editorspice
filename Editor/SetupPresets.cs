using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     From Warped Imagination: https://www.youtube.com/watch?v=KFmP1Q8NySo
    /// </summary>
    public static class SetupPresets
    {
        private static string _presetsFilter => "t:preset";


        private static string[] _foldersToSearch
        {
            get { return new[] {"Assets/_SOSXR", "Packages"}; }
        }


        [MenuItem("SOSXR/Setup Presets")]
        private static void SetupLibraryPresetsMenuOption()
        {
            var guids = AssetDatabase.FindAssets(_presetsFilter, _foldersToSearch);


            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                var preset = AssetDatabase.LoadAssetAtPath<Preset>(path);

                if (!preset.name.Contains("Default"))
                {
                    return;
                }

                var type = preset.GetPresetType();

                var list = new List<DefaultPreset>(Preset.GetDefaultPresetsForType(type));

                if (list.Any(defaultPreset => defaultPreset.preset == preset))
                {
                    return;
                }

                list.Add(new DefaultPreset(null, preset));

                Preset.SetDefaultPresetsForType(type, list.ToArray());
            }
        }
    }
}