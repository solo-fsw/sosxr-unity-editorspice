using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     From Warped Imagination @ https://youtu.be/Rdg0PQS5OiU?si=sRTkgurRIXPfq_rv
    ///     Superseded by ToggleUsingHierarchyIcon.cs, which does the same thing but with the icon instead of a toggle
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyToggleButton
    {
        static HierarchyToggleButton()
        {
            // EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }


        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null)
            {
                return;
            }

            var rect = new Rect(selectionRect);
            rect.x -= 27f; // 27f is given by Warped Imagination
            rect.width = 13f; // 13f is given by Warped Imagination 

            var active = EditorGUI.Toggle(rect, gameObject.activeSelf);

            if (active == gameObject.activeSelf)
            {
                return;
            }

            Undo.RecordObject(gameObject, "Active state change");
            gameObject.SetActive(active);

            if (Application.isPlaying)
            {
                return;
            }

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }
}