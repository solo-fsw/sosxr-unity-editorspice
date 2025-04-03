using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice
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
                menu.AddItem(new GUIContent("Zero Out"), false, () =>
                {
                    property.vector3Value = Vector3.zero;
                    property.serializedObject.ApplyModifiedProperties();
                });

                menu.AddItem(new GUIContent("One"), false, () =>
                {
                    property.vector3Value = Vector3.one;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
        }
    }
    
    
}