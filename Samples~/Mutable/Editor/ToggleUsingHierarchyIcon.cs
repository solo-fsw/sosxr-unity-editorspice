using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     Click the icon in the hierarchy to toggle the active state of the object
    ///     From Warped Imagination: https://youtu.be/0Wu_vz5WVck?si=GSavz27kLZC3PKzU
    /// </summary>
    [InitializeOnLoad]
    public class ToggleUsingHierarchyIcon
    {
        static ToggleUsingHierarchyIcon()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }


        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (obj == null)
            {
                return;
            }

            var iconSize = 15;

            var rect = new Rect(selectionRect.x, selectionRect.y, iconSize, selectionRect.height);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                if (!Application.isPlaying)
                {
                    Undo.RecordObject(obj, "Changing active state of object");
                }

                obj.SetActive(!obj.activeSelf);

                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(obj);
                }

                Event.current.Use();
            }
        }
    }
}