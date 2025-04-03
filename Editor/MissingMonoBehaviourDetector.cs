using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    public class MissingMonoBehaviourDetector : MonoBehaviour
    {
        // Optional: [UnityEditor.Callbacks.DidReloadScripts]
        [MenuItem("SOSXR/Find GameObjects with Missing Scripts")]
        [ContextMenu(nameof(FindGameObjectsWithMissingScripts))]
        private static void FindGameObjectsWithMissingScripts()
        {
            var allObjectsInScene = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

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
            var components = gameObject.GetComponents<Component>();
            var serializedObject = new SerializedObject(gameObject);
            var prop = serializedObject.FindProperty("m_Component");
            var r = 0;

            for (var j = 0; j < components.Length; j++)
            {
                if (components[j] != null)
                {
                    continue;
                }

                prop.DeleteArrayElementAtIndex(j - r);
                r++;
            }

            if (r <= 0)
            {
                return false;
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            return true;
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