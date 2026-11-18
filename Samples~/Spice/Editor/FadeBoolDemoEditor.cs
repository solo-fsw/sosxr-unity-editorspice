using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;


namespace SOSXR.EditorSpice.EditorScripts.Samples.Editor
{
    /// <summary>
    /// Purpose: Custom editor for the FadeBoolDemo to demonstrate a fadeable UI region in the Inspector.
    /// Use Case: Learn how to reveal/hide inspector fields based on a boolean condition using AnimBool.
    /// How It Works: Binds to the FadeBoolDemo and renders an animated foldout area via AnimBoolDropdown/BeginFadeGroup.
    /// Integration: Part of EditorSpice samples illustrating editor extension patterns.
    /// Related Classes: FadeBoolDemo, FadeBoolDemoEditor.
    /// </summary>
    [CustomEditor(typeof(FadeBoolDemo))]
    public class FadeBoolDemoEditor : UnityEditor.Editor
    {
        private AnimBool _showThings;


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
