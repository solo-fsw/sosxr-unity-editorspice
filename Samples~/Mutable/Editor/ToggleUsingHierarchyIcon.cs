using UnityEditor;
using UnityEngine;
#if UNITY_6000_5_OR_NEWER
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEngine.UIElements;
#endif


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    /// Purpose: Toggle a GameObject's active state by clicking a hierarchy icon.
    ///
    /// Use Case: Fast activation/deactivation during scene editing without opening the Inspector.
    ///
    /// How It Works: Legacy Hierarchy: subscribes to EditorApplication.hierarchyWindowItemByEntityIdOnGUI (IMGUI).
    /// New Hierarchy (Unity 6.5+): subscribes to HierarchyWindow.BindViewItem (UI Toolkit) and adds a pointer handler
    /// to each item that toggles when the click lands on the item's icon. The IMGUI callback never fires in the new Hierarchy window.
    /// Both paths record undo and mark the object dirty when not in Play mode.
    ///
    /// Integration: Editor-spice sample demonstrating common Editor scripting patterns.
    /// Related Classes: HierarchyIconDisplay, HierarchyToggleButton.
    /// </summary>
    [InitializeOnLoad]
    public class ToggleUsingHierarchyIcon
    {
        static ToggleUsingHierarchyIcon()
        {
            // Legacy Hierarchy window (IMGUI). Never called by the new Hierarchy window.
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HandleHierarchyWindowItemOnGUI;
#if UNITY_6000_5_OR_NEWER
            HierarchyWindow.BindViewItem += HandleBindViewItem;

            // Items already bound before this subscription won't fire BindViewItem again until rebound.
            EditorApplication.delayCall += RegisterOnExistingItems;
#endif
        }


#if UNITY_6000_5_OR_NEWER
        private const string IconToggleUssClass = "sosxr-icon-toggle";


        private static void RegisterOnExistingItems()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            {
                window.View?.Query<HierarchyViewItem>().ForEach(RegisterOnItem);
            }
        }


        private static void HandleBindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item)
        {
            RegisterOnItem(item);
        }


        private static void RegisterOnItem(HierarchyViewItem item)
        {
            // View items are pooled and rebound. Register the click handler only once per item.
            if (item.ClassListContains(IconToggleUssClass))
            {
                return;
            }

            item.AddToClassList(IconToggleUssClass);

            // Registered on the whole item (TrickleDown), not on item.Icon: the overlay icon element sits on top of
            // the icon and receives the pointer instead. Hit-test the icon bounds ourselves.
            item.RegisterCallback<PointerDownEvent>(HandleItemPointerDown, TrickleDown.TrickleDown);
        }


        private static void HandleItemPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            if (evt.currentTarget is not HierarchyViewItem item)
            {
                return;
            }

            if (!item.Icon.worldBound.Contains(evt.position))
            {
                return;
            }

            // Read the GameObject at click time: the item may have been rebound to another node since binding.
            if (item.Handler is not HierarchyGameObjectHandler handler)
            {
                return;
            }

            var obj = handler.GetGameObject(item.Node);

            if (obj == null)
            {
                return;
            }

            ToggleActive(obj);
            evt.StopImmediatePropagation();
        }
#endif


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
                ToggleActive(obj);
                Event.current.Use();
            }
        }


        private static void ToggleActive(GameObject obj)
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
        }
    }
}
