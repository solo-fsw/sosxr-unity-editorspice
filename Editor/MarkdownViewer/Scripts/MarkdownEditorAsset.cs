using UnityEditor;
using UnityEngine;


namespace MG.MDV
{
    [CustomEditor(typeof(MarkdownAsset))]
    public class MarkdownEditorAsset : Editor
    {
        public GUISkin SkinLight;
        public GUISkin SkinDark;

        private MarkdownViewer mViewer;


        protected void OnEnable()
        {
            var content = (target as MarkdownAsset).text;
            var path = AssetDatabase.GetAssetPath(target);

            mViewer = new MarkdownViewer(Preferences.DarkSkin ? SkinDark : SkinLight, path, content);
            EditorApplication.update += UpdateRequests;
        }


        protected void OnDisable()
        {
            EditorApplication.update -= UpdateRequests;
            mViewer?.Dispose();
            mViewer = null;
        }


        public override bool UseDefaultMargins()
        {
            return false;
        }


        protected override void OnHeaderGUI()
        {
            //base.OnHeaderGUI(); 
        }


        public override void OnInspectorGUI()
        {
            mViewer.Draw();
        }


        private void UpdateRequests()
        {
            if (mViewer.Update())
            {
                Repaint();
            }
        }
    }
}