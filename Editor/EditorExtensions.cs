using System.IO;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     Provides extension methods for various editor functionalities.
    /// </summary>
    public static class EditorExtensions
    {
        /// <summary>
        ///     Checks if a file exists at the specified path and prompts the user for confirmation to overwrite it.
        ///     From: https://github.com/adammyhre/Unity-Utils
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>True if the file does not exist or the user confirms to overwrite it; otherwise, false.</returns>
        public static bool ConfirmOverwrite(this string path)
        {
            if (File.Exists(path))
            {
                return EditorUtility.DisplayDialog
                (
                    "File Exists",
                    "The file already exists at the specified path. Do you want to overwrite it?",
                    "Yes",
                    "No"
                );
            }

            return true;
        }


        /// <summary>
        ///     Opens a folder browser dialog and returns the selected folder path.
        ///     From: https://github.com/adammyhre/Unity-Utils
        /// </summary>
        /// <param name="defaultPath">The default path to open the folder browser at.</param>
        /// <returns>The selected folder path.</returns>
        public static string BrowseForFolder(this string defaultPath)
        {
            return EditorUtility.SaveFolderPanel
            (
                "Choose Save Path",
                defaultPath,
                ""
            );
        }


        /// <summary>
        ///     Pings and selects the specified asset in the Unity Editor.
        ///     From: https://github.com/adammyhre/Unity-Utils
        /// </summary>
        /// <param name="asset">The asset to ping and select.</param>
        public static void PingAndSelect(this Object asset)
        {
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }


        /// <summary>
        ///     This creates a dropdown with an icon for an AnimBool.
        ///     Make sure to wrap it in a EditorGUILayout.BeginFadeGroup(animBool.faded) and EditorGUILayout.EndFadeGroup().
        ///     Inspired by Warped Imagination: https://www.youtube.com/watch?v=VRp-34qvOP8&t=43s
        /// </summary>
        /// <param name="animBool"></param>
        /// <param name="labelName"></param>
        public static void AnimBoolDropdown(this AnimBool animBool, string labelName = "SOSXR")
        {
            var current = animBool.target;

            var toggle = current ? EditorGUIUtility.IconContent("d_icon dropdown@2x") : EditorGUIUtility.IconContent("d_PlayButton@2x");

            GUILayout.BeginHorizontal();

            var savedColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;

            if (GUILayout.Button(toggle, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                current = !current;
            }

            GUI.backgroundColor = savedColor;
            GUILayout.Label(labelName, EditorStyles.boldLabel);

            GUILayout.EndHorizontal();

            animBool.target = current;
        }
    }
}