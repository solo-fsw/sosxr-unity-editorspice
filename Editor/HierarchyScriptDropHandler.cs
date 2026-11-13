using System;
using UnityEditor;
using UnityEngine;

namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     From Warped Imagination: https://www.youtube.com/watch?v=FpOAcfULmTE
    /// </summary>
    [InitializeOnLoad]
    public class HierarchyScriptDropHandler
    {
        static HierarchyScriptDropHandler() => DragAndDrop.AddDropHandlerV2(OnScriptHierarchyDrop);

        private static DragAndDropVisualMode OnScriptHierarchyDrop(EntityId dropTargetEntityId, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            var targetIsGameObject = EditorUtility.EntityIdToObject(dropTargetEntityId) is GameObject;

            // Dropping onto a GameObject: let Unity's default handler add the component to it
            if (!ShouldHandleDrop(targetIsGameObject, dropMode))
            {
                return DragAndDropVisualMode.None;
            }

            var monoScript = GetScriptBeingDragged();

            if (monoScript != null)
            {
                if (perform)
                {
                    var gameObject = CreateAndRename(monoScript.name);
                    _ = gameObject.AddComponent(monoScript.GetClass());
                }

                return DragAndDropVisualMode.Copy;
            }

            return DragAndDropVisualMode.None;
        }

        /// <summary>
        ///     True when this handler should create a new GameObject for the dropped script.
        ///     False when the script is dropped onto an existing GameObject.
        /// </summary>
        public static bool ShouldHandleDrop(bool targetIsGameObject, HierarchyDropFlags dropMode)
        {
            return !(targetIsGameObject && (dropMode & HierarchyDropFlags.DropUpon) != 0);
        }


        public static GameObject CreateAndRename(string startingName)
        {
            GameObject gameObject = new(startingName);

            if (Selection.activeGameObject != null)
            {
                gameObject.transform.parent = Selection.activeGameObject.transform;
            }
            else
            {
                gameObject.transform.SetParent(null); // Or another default parent
            }

            gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            Selection.activeGameObject = gameObject;

            Undo.RegisterCreatedObjectUndo(gameObject, "Created GameObject");

            EditorApplication.delayCall += static () => FrameAndRenameInHierarchy();

            return gameObject;
        }

        /// <summary>
        ///     Frames the selected GameObject in the Hierarchy and starts renaming it.
        ///     Works with both the legacy SceneHierarchyWindow and the new (Unity 6.3+) HierarchyWindow.
        ///     Uses Unity's own internal "frame and rename new GameObject" hook, the same one that
        ///     GameObject > Create Empty uses. Both Hierarchy implementations subscribe to it.
        ///     Never calls EditorWindow.GetWindow: that would open a legacy Hierarchy when the new one is in use.
        /// </summary>
        private static void FrameAndRenameInHierarchy()
        {
            var frameAndRename = FindFrameAndRenameHook();

            if (frameAndRename != null)
            {
                _ = frameAndRename.Invoke(null, null);

                return;
            }

            // Fallback if Unity renames the internal hook: send a Rename command to an already open Hierarchy.
            var hierarchy = FindOpenHierarchyWindow();

            if (hierarchy == null)
            {
                return;
            }

            hierarchy.Focus();
            _ = hierarchy.SendEvent(EditorGUIUtility.CommandEvent("Rename"));
        }

        /// <summary>
        ///     Unity's internal hook behind GameObject > Create Empty's frame-and-rename. Null if Unity renamed or removed it.
        /// </summary>
        public static System.Reflection.MethodInfo FindFrameAndRenameHook()
        {
            return typeof(EditorApplication).GetMethod("Internal_FrameAndRenameNewGameObject",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        }


        private static EditorWindow FindOpenHierarchyWindow()
        {
            var newHierarchies = Resources.FindObjectsOfTypeAll<Unity.Hierarchy.Editor.HierarchyWindow>();

            if (newHierarchies.Length > 0)
            {
                return newHierarchies[0];
            }

            var legacyType = Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor");

            if (legacyType == null)
            {
                return null;
            }

            var legacyHierarchies = Resources.FindObjectsOfTypeAll(legacyType);

            return legacyHierarchies.Length > 0 ? legacyHierarchies[0] as EditorWindow : null;
        }

        private static MonoScript GetScriptBeingDragged()
        {
            foreach (var objectReference in DragAndDrop.objectReferences)
            {
                if (objectReference is MonoScript monoScript)
                {
                    var scriptType = monoScript.GetClass();

                    if (scriptType != null && scriptType.IsSubclassOf(typeof(MonoBehaviour)))
                    {
                        return monoScript;
                    }
                }
            }

            return null;
        }
    }
}
