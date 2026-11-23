using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Menu items to find and remove missing scripts. A static class (not a MonoBehaviour): it lives in an Editor-only
    ///     assembly, so as a component it would be missing in builds.
    /// </summary>
    public static class MissingMonoBehaviourDetector
    {
        // Optional: [UnityEditor.Callbacks.DidReloadScripts]
        [MenuItem("SOSXR/Find GameObjects with Missing Scripts")]
        private static void FindGameObjectsWithMissingScripts()
        {
            // Include inactive objects: a missing script on a disabled object breaks just the same once it is enabled.
            var allObjectsInScene = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

            foreach (var go in allObjectsInScene)
            {
                var allMonoBehavioursInScene = go.GetComponents<MonoBehaviour>();

                foreach (var monoBehaviour in allMonoBehavioursInScene)
                {
                    if (monoBehaviour != null)
                    {
                        continue;
                    }

                    string location = null;

                    if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)))
                    {
                        location = $"(Prefab: {AssetDatabase.GetAssetPath(go)}) ";
                    }

                    if (!GetFullPath(go.transform.parent).Equals("(Missing Transform)"))
                    {
                        location += $" / (Hierarchy: {GetFullPath(go.transform.parent)})";
                    }

                    Debug.LogWarning($"SOSXR: Missing MonoBehaviour found on GameObject '{go.name}' {location}");
                }
            }
        }


        [MenuItem("SOSXR/DANGER/Remove Missing Scripts From Selected GameObjects")]
        public static void RemoveMissingScripts()
        {
            var gameObjects = Selection.gameObjects;
            var count = 0;

            foreach (var gameObject in gameObjects)
            {
                if (!RemoveMissingScripts(gameObject))
                {
                    continue;
                }

                count++;
            }

            Debug.Log($"SOSXR: Removed missing scripts from {count} GameObject(s).");
        }


        private static bool RemoveMissingScripts(GameObject gameObject)
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject) == 0)
            {
                return false;
            }

            // Record first, so Edit > Undo brings the (missing) components back.
            Undo.RegisterCompleteObjectUndo(gameObject, "Remove Missing Scripts");

            return GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject) > 0;
        }


        private static bool IsPartOfPrefab(GameObject gameObject)
        {
            return AssetDatabase.Contains(gameObject);
        }


        private static string GetFullPath(Transform transform)
        {
            if (transform == null)
            {
                return "(Missing Transform)";
            }

            return transform.parent == null ? transform.name : GetFullPath(transform.parent) + "/" + transform.name;
        }
    }
}