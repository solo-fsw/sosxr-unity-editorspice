using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     From Warped Imagination: https://www.youtube.com/watch?v=hBrLsyLGaB4
    /// </summary>
    public static class ContextProperties
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }


        private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            Vector3ContextMenu(menu, property);
        }


        private static void Vector3ContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Vector3)
            {
                // The menu runs later: keep a copy, because Unity reuses the property passed in.
                var propertyCopy = property.Copy();

                menu.AddItem(new GUIContent("Zero Out"), false, () =>
                {
                    propertyCopy.vector3Value = Vector3.zero;
                    propertyCopy.serializedObject.ApplyModifiedProperties();
                });

                menu.AddItem(new GUIContent("One"), false, () =>
                {
                    propertyCopy.vector3Value = Vector3.one;
                    propertyCopy.serializedObject.ApplyModifiedProperties();
                });
            }
        }
    }
}