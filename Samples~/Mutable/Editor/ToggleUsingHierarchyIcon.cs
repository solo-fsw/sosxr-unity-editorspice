using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    /// Purpose: Toggle a GameObject's active state by clicking a hierarchy icon.
    ///
    /// Use Case: Fast activation/deactivation during scene editing without opening the Inspector.
    ///
    /// How It Works: Subscribes to EditorApplication.hierarchyWindowItemOnGUI and toggles active state on click;
    /// records undo and marks the object dirty when not in Play mode.
    ///
    /// Integration: Editor-spice sample demonstrating common Editor scripting patterns.
    /// Related Classes: HierarchyIconDisplay, HierarchyToggleButton.
    /// </summary>
    [InitializeOnLoad]
    public class ToggleUsingHierarchyIcon
    {
        static ToggleUsingHierarchyIcon()
        {
#if UNITY_6000_3_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HandleHierarchyWindowItemOnGUI;
#else
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
#endif
        }


        private static void HandleHierarchyWindowItemOnGUI(UnityEngine.EntityId instanceID, Rect selectionRect)
        {
            var obj = EditorUtility.EntityIdToObject(instanceID) as GameObject;

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
