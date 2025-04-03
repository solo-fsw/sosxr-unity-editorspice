using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;


namespace SOSXR.EditorSpice.Samples.Editor
{
    [CustomEditor(typeof(FadeBoolDemo))]
    public class FadeBoolDemoEditor : UnityEditor.Editor
    {
        private AnimBool _showThings = null;


        private void OnEnable()
        {
            _showThings = new AnimBool();
            _showThings.valueChanged.AddListener(Repaint);
        }


        public override void OnInspectorGUI()
        {
            _showThings.AnimBoolDropdown("Show Things");

            if (EditorGUILayout.BeginFadeGroup(_showThings.faded))
            {
                GUILayout.Label("Things");
                GUILayout.Label("Thing 1");
                GUILayout.Label("Thing 2");
                GUILayout.Label("Thing 3");
            }

            EditorGUILayout.EndFadeGroup();
        }
    }
}