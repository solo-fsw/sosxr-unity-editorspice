using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace MG.MDV
{
    [CustomEditor(typeof(TextAsset))]
    public class MarkdownEditor : Editor
    {
        public GUISkin SkinLight;
        public GUISkin SkinDark;

        private static readonly List<string> mExtensions = new() {".md", ".markdown"};

        private MarkdownViewer mViewer;


        //------------------------------------------------------------------------------

        private Editor mDefaultEditor;


        protected void OnEnable()
        {
            var path = AssetDatabase.GetAssetPath(target);

            var ext = Path.GetExtension(path).ToLower();

            if (mExtensions.Contains(ext))
            {
                // Read .text only for markdown files. Other TextAssets can be large (json, csv, bytes).
                var content = (target as TextAsset).text;
                mViewer = new MarkdownViewer(Preferences.DarkSkin ? SkinDark : SkinLight, path, content);
                EditorApplication.update += UpdateRequests;
            }
        }


        protected void OnDisable()
        {
            if (mViewer != null)
            {
                EditorApplication.update -= UpdateRequests;
                mViewer.Dispose();
                mViewer = null;
            }

            if (mDefaultEditor != null)
            {
                DestroyImmediate(mDefaultEditor);
                mDefaultEditor = null;
            }
        }


        private void UpdateRequests()
        {
            if (mViewer != null && mViewer.Update())
            {
                Repaint();
            }
        }


        //------------------------------------------------------------------------------


        public override bool UseDefaultMargins()
        {
            return false;
        }


        protected override void OnHeaderGUI()
        {
            #if UNITY_2019_2_OR_NEWER && !UNITY_2020_1_OR_NEWER
            // TODO: workaround for bug in 2019.2
            // https://forum.unity.com/threads/oninspectorgui-not-being-called-on-defaultasset-in-2019-2-0f1.724328/
            DrawEditor();
            #endif
        }


        public override void OnInspectorGUI()
        {
            #if !UNITY_2019_2_OR_NEWER || UNITY_2020_1_OR_NEWER
            DrawEditor();
            #endif
        }


        private void DrawEditor()
        {
            if (mViewer != null)
            {
                mViewer.Draw();
            }
            else
            {
                DrawDefaultEditor();
            }
        }


        private void DrawDefaultEditor()
        {
            if (mDefaultEditor == null)
            {
                mDefaultEditor = CreateEditor(target, Type.GetType("UnityEditor.TextAssetInspector, UnityEditor"));
            }

            if (mDefaultEditor != null)
            {
                mDefaultEditor.OnInspectorGUI();
            }
        }
    }
}