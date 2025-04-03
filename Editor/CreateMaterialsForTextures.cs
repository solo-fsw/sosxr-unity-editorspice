using System;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     From: https://answers.unity.com/questions/745917/is-it-possible-to-automate-material-creation-with.html
    /// </summary>
    public class CreateMaterialsForTextures : ScriptableWizard
    {
        public Shader Shader;


        [MenuItem("SOSXR/Create Materials for Selected Textures")]
        private static void CreateWizard()
        {
            DisplayWizard<CreateMaterialsForTextures>("Create Materials", "Create");
        }


        private void OnEnable()
        {
            Shader = Shader.Find("SimpleLit");
        }


        private void OnWizardCreate()
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                var textures = Selection.GetFiltered(typeof(Texture), SelectionMode.Assets).Cast<Texture>();

                foreach (var tex in textures)
                {
                    var path = AssetDatabase.GetAssetPath(tex);
                    path = path[..path.LastIndexOf(".", StringComparison.Ordinal)] + ".mat";

                    if (AssetDatabase.LoadAssetAtPath(path, typeof(Material)) != null)
                    {
                        Debug.LogWarning("Can't create material, it already exists: " + path);

                        continue;
                    }

                    var mat = new Material(Shader)
                    {
                        mainTexture = tex
                    };

                    AssetDatabase.CreateAsset(mat, path);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }
        }
    }
}