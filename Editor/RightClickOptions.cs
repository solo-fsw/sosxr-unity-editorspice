using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    /// Editor utility class that exposes context menu actions for Components in the Inspector.
    /// </summary>
    public static class RightClickOptions
    {
        /// <summary>
        /// Purpose: Copy the current component to a new child GameObject and remove the original component.
        /// Use Case: Quick component duplication as a child while preserving the original state.
        /// How It Works: Creates a new child GameObject, copies the source component to the child, and removes
        /// the original component from the source object. All operations are registered with Undo for editor safety.
        /// Integration: Invoked via the component context menu item "Extract Component to Child". Can be extended with
        /// additional context menu actions as needed.
        /// Related Classes: Undo, ComponentUtility, GameObject, Component.
        /// </summary>
        /// <param name="command">The MenuCommand containing the target component to extract.</param>
        [MenuItem("CONTEXT/Component/Extract Component to Child", priority = 555)]
        public static void ExtractMenuOption(MenuCommand command)
        {
            var source = command.context as Component;

            if (source == null || source is Transform)
            {
                return; // A Transform cannot move to another GameObject
            }

            var undoGroupIndex = Undo.GetCurrentGroup();

            Undo.IncrementCurrentGroup();

            var child = new GameObject(source.GetType().Name);
            // The child sits exactly on its parent: local zero, identity and one. Copying the parent's local values offset it.
            child.transform.SetParent(source.transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            Undo.RegisterCreatedObjectUndo(child, $"Created child object: {child.name}");

            if (!ComponentUtility.CopyComponent(source) || !ComponentUtility.PasteComponentAsNew(child))
            {
                Debug.LogErrorFormat(source.gameObject, "SOSXR: Failed to extract component");

                Undo.CollapseUndoOperations(undoGroupIndex);
                Undo.PerformUndo();

                return;
            }

            Undo.DestroyObjectImmediate(source);
            Undo.CollapseUndoOperations(undoGroupIndex);
        }
    }
}
