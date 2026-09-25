using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    /// Purpose: Show an editor-only icon overlay in the Hierarchy for objects based on their components.
    ///
    /// Use Case: Useful during learning or debugging to quickly identify an object's primary component
    /// without opening the Inspector.
    ///
    /// How It Works: Hooks into EditorApplication.hierarchyWindowItemOnGUI to render an icon next to GameObjects
    /// by selecting the most relevant component type.
    ///
    /// Integration: Editor-only utility. Drop into an Editor folder; enabled at editor load.
    /// Related Classes: ToggleUsingHierarchyIcon, HierarchyToggleButton, AudioSourceExtendedEditor.
    /// </summary>
    [InitializeOnLoad] // This will call the constructor of the class when Unity starts
    public static class HierarchyIconDisplay
    {
        static HierarchyIconDisplay()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyWindowItemOnGUI;
            EditorApplication.update += OnEditorUpdate;
        }

        private static readonly bool IncludeScripts = true;
        private static readonly bool KeepIconsForPrefabs = false;

        private static bool _hierarchyHasFocus;
        private static EditorWindow _hierarchyEditorWindow;

        /// <summary>
        ///     Runs every time the editor updates
        /// </summary>
        private static void OnEditorUpdate()
        {
            if (_hierarchyEditorWindow == null)
            {
                _hierarchyEditorWindow = EditorWindow.GetWindow(Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor"));
            }

            _hierarchyHasFocus = EditorWindow.focusedWindow != null && EditorWindow.focusedWindow == _hierarchyEditorWindow;
        }

        private static void OnHierarchyWindowItemOnGUI(UnityEngine.EntityId instanceID, Rect selectionRect)
        {
            var gameObject = EditorUtility.EntityIdToObject(instanceID) as GameObject;

            if (gameObject == null)
            {
                return;
            }

            if (PrefabUtility.GetCorrespondingObjectFromSource(gameObject) != null && KeepIconsForPrefabs) // Keep icons for prefabs
            {
                return;
            }

            var components = gameObject.GetComponents<Component>();

            if (components == null || components.Length == 0)
            {
                return;
            }

            GUIContent content;
            Component component = null; // components[0] is the Transform. This does mean that the 'most interesting / important' component should be the first one that is not the Transform component

            if (components.Length > 1)
            {
                component = components[1];
            }
            else
            {
                component = components[0];
            }

            if (component == null)
            {
                return;
            }

            var type = component.GetType();

            var gameObjectIcon = EditorGUIUtility.GetIconForObject(gameObject);

            if (components.Length == 1 && gameObjectIcon != null)
            {
                content = new GUIContent(gameObjectIcon) { tooltip = "GameObject Icon" };
            }
            else if (!IncludeScripts)
            {
                content = EditorGUIUtility.ObjectContent(null, type); // Gimme the icon for the particular content.
            }
            else
            {
                content = EditorGUIUtility.ObjectContent(component, type); // Gimme the icon for the particular content.
            }

            content.text = null;
            content.tooltip = type.Name; // Set the tooltip to the name of the type

            if (content.image == null)
            {
                // maybe log me some stuff
                return;
            }

#if UNITY_6000_3_OR_NEWER
            var isSelected = Selection.entityIds.Contains(instanceID);
#else
            var isSelected = Selection.instanceIDs.Contains(instanceID);
#endif
            var isHovering = selectionRect.Contains(Event.current.mousePosition);

            var color = UnityEditorBackgroundColor.Get(isSelected, isHovering, _hierarchyHasFocus);
            var backgroundRect = selectionRect;
            backgroundRect.width = 18.5f; // 18.5f is given by Warped Imagination
            EditorGUI.DrawRect(backgroundRect, color);

            EditorGUI.LabelField(selectionRect, content);
        }
    }
}
