using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


// From: [talecrafter](https://gist.github.com/talecrafter/519e260d93dbf236484acfe625faa1dc)
namespace SOSXR.EditorSpice.EditorScripts
{
    public static class PivotUtilities
    {
        [MenuItem("SOSXR/Pivot/Create Pivot", false, 0)]
        private static void CreatePivotObject()
        {
            if (Selection.activeGameObject != null)
            {
                var undoGroup = Undo.GetCurrentGroup();
                var pivot = CreatePivotObject(Selection.activeGameObject);
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeGameObject = pivot;
            }
        }


        [MenuItem("SOSXR/Pivot/Create Pivot (Local Zero)", false, 0)]
        private static void CreatePivotObjectAtParentPos()
        {
            if (Selection.activeGameObject != null)
            {
                var undoGroup = Undo.GetCurrentGroup();
                var pivot = CreatePivotObjectAtParentPos(Selection.activeGameObject);
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeGameObject = pivot;
            }
        }


        [MenuItem("SOSXR/Pivot/Delete Pivot", false, 0)]
        private static void DeletePivotObject()
        {
            GameObject objSelectionAfter = null;

            var selected = Selection.activeGameObject;

            if (selected == null)
            {
                return;
            }

            // A pivot is an empty parent. Without children this would just delete the selected object.
            if (selected.transform.childCount == 0)
            {
                Debug.LogWarning($"{selected.name} has no children, so it is not a pivot. Nothing was deleted.", selected);

                return;
            }

            if (selected.GetComponents<Component>().Length > 1
                && !EditorUtility.DisplayDialog("Delete Pivot", $"{selected.name} has components besides its Transform. Delete it and keep its children?", "Delete", "Cancel"))
            {
                return;
            }

            objSelectionAfter = selected.transform.GetChild(0).gameObject;

            var undoGroup = Undo.GetCurrentGroup();
            DeletePivotObject(selected);
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = objSelectionAfter;
        }


        private static GameObject CreatePivotObjectAtParentPos(GameObject current)
        {
            if (current == null)
            {
                return null;
            }

            var siblingIndex = current.transform.GetSiblingIndex();

            var newObject = CreateUndoablePivot(current);

            newObject.transform.localPosition = Vector3.zero;
            newObject.transform.localScale = Vector3.one;
            newObject.transform.localRotation = Quaternion.identity;

            newObject.transform.SetSiblingIndex(siblingIndex);

            Undo.SetTransformParent(current.transform, newObject.transform, "Create Pivot");

            return newObject;
        }


        /// <summary>Creates an empty "Pivot" next to <paramref name="current" /> (same parent and scene), registered for Undo.</summary>
        private static GameObject CreateUndoablePivot(GameObject current)
        {
            var newObject = new GameObject("Pivot");

            // Without a parent a new GameObject lands in the active scene: keep it in the scene of the selected object.
            if (current.transform.parent == null && current.scene.IsValid() && newObject.scene != current.scene)
            {
                SceneManager.MoveGameObjectToScene(newObject, current.scene);
            }

            newObject.transform.SetParent(current.transform.parent);
            Undo.RegisterCreatedObjectUndo(newObject, "Create Pivot");

            return newObject;
        }


        private static GameObject CreatePivotObject(GameObject current)
        {
            if (current == null)
            {
                return null;
            }

            var siblingIndex = current.transform.GetSiblingIndex();

            var newObject = CreateUndoablePivot(current);

            newObject.transform.position = current.transform.position;
            newObject.transform.localScale = current.transform.localScale;
            newObject.transform.rotation = current.transform.rotation;

            newObject.transform.SetSiblingIndex(siblingIndex);

            Undo.SetTransformParent(current.transform, newObject.transform, "Create Pivot");

            return newObject;
        }


        private static GameObject DeletePivotObject(GameObject current)
        {
            var parent = current.transform.parent;
            var childrenCount = current.transform.childCount;
            var siblingIndex = current.transform.GetSiblingIndex();

            var children = new Transform[childrenCount];

            for (var i = 0; i < childrenCount; i++)
            {
                children[i] = current.transform.GetChild(i);
            }

            for (var i = 0; i < childrenCount; i++)
            {
                Undo.SetTransformParent(children[i], parent, "Delete Pivot");
                children[i].SetSiblingIndex(siblingIndex + i);
            }

            if (Application.isPlaying)
            {
                GameObject.Destroy(current);
            }
            else
            {
                Undo.DestroyObjectImmediate(current);
            }

            if (children.Length > 0)
            {
                return children[0].gameObject;
            }

            return null;
        }
    }
}