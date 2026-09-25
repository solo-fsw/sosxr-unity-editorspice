using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    /// Purpose: Toggle a GameObject's active state from the Hierarchy (legacy approach).
    /// 
    /// Use Case: Provides a quick toggle while editing scenes without opening the Inspector.
    /// 
    /// How It Works: Listens to hierarchy GUI to flip GameObject.activeSelf and marks the scene dirty when appropriate.
    /// 
    /// Integration: Demonstrates an Editor extension pattern alongside ToggleUsingHierarchyIcon.
    /// Related Classes: ToggleUsingHierarchyIcon, HierarchyIconDisplay.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyToggleButton
    {
        static HierarchyToggleButton()
        {
            // EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }


        private static void HandleHierarchyWindowItemOnGUI(UnityEngine.EntityId instanceID, Rect selectionRect)
        {
            var gameObject = EditorUtility.EntityIdToObject(instanceID) as GameObject;

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
