using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     Create a custom GameObject with icons for better organization. It promotes separating out different types of
    ///     components. This is especially useful when wanting to keep the Model of a thing separate from the rest of the
    ///     components.
    ///     Use the shortcut: Ctrl/Cmd + Alt + Shift + N
    /// </summary>
    public class CreateCustomGameObject : Editor
    {
        [MenuItem("GameObject/Create Empty GameObject _&#%N", false, 0)]
        private static void CreateCustomGameObjectFunction()
        {
            var parent = CreateGameObject("Parent GameObject", "SOSXR_Logo");

            var animation = CreateGameObject("Animation", "Animation_icon", parent);

            CreateGameObject("Model", "Model_icon", animation);

            CreateGameObject("Scripts", "Code_icon", parent);

            CreateGameObject("Audio", "Audio_icon", parent);

            CreateGameObject("Lights", "Lamp_icon", parent);

            CreateGameObject("VFX", "VFX_icon", parent);

            Debug.Log("SOSXR: Custom GameObject created.");
        }


        private static GameObject CreateGameObject(string goName, string iconName, GameObject parentTo = null)
        {
            var go = new GameObject(goName);
            go.transform.parent = parentTo?.transform;
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            var icon = Resources.Load<Texture2D>(iconName);

            if (icon != null)
            {
                EditorGUIUtility.SetIconForObject(go, icon);
            }

            return go;
        }
    }
}