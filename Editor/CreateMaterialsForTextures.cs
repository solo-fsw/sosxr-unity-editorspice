using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace SOSXR.EditorSpice.EditorScripts
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
            Shader = FindDefaultShader();
        }


        /// <summary>
        ///     The default shader of the active render pipeline (for example URP Lit), or the Built-in Standard shader.
        ///     Shader.Find("SimpleLit") found nothing: the URP name is "Universal Render Pipeline/Simple Lit".
        /// </summary>
        private static Shader FindDefaultShader()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;

            if (pipeline != null && pipeline.defaultShader != null)
            {
                return pipeline.defaultShader;
            }

            return Shader.Find("Standard");
        }


        private void OnWizardCreate()
        {
            if (Shader == null)
            {
                Debug.LogError("No shader selected. Pick a shader in the wizard and try again.");

                return;
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                var textures = Selection.GetFiltered(typeof(Texture), SelectionMode.Assets).Cast<Texture>();

                foreach (var tex in textures)
                {
                    var path = AssetDatabase.GetAssetPath(tex);
                    path = System.IO.Path.ChangeExtension(path, ".mat");

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