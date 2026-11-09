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

            EditorApplication.delayCall += static () =>
            {
                Type sceneHierarchyType = Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor");
                _ = EditorWindow.GetWindow(sceneHierarchyType).SendEvent(EditorGUIUtility.CommandEvent("Rename"));
            };

            return gameObject;
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
