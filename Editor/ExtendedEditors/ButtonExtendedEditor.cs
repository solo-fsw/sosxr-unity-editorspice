// No longer needed, as the functionality is now in the UnityEventDrawerWithButton.cs file

/*

using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     Based on: Warped Imagination
    ///     https://www.youtube.com/watch?v=iqbUbtwiiz0
    /// </summary>
    [CustomEditor(typeof(Button))]
    public class ButtonExtendedEditor : EditorGUIHelpers
    {
        private void OnEnable()
        {
            GetInternalEditor(typeof(ButtonEditor));
        }


        protected override void CustomInspectorContent()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("SOSXR: Click Button", GUILayout.Width(ButtonWidth)))
            {
                var button = (Button) target;

                button.onClick?.Invoke();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }

}

*/

