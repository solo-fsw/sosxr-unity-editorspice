using System;
using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    // [CustomEditor(typeof(Transform), true)]
    public class CustomInspectorExtension : Editor
    {
        private bool _moveToChildren = true;

        protected Editor InternalEditor;


        private void OnEnable()
        {
            GetInternalEditor("TransformInspector");
        }


        public override void OnInspectorGUI()
        {
            InternalEditor.OnInspectorGUI();

            if (!IsRoot())
            {
                return;
            }

            _moveToChildren = GUILayout.Toggle(_moveToChildren, "Move Components to Children");

            if (ComponentCount() <= 1) // Only the Transform component is present
            {
                return;
            }

            if (_moveToChildren)
            {
                var currentComponents = GetComponents();

                foreach (var component in currentComponents)
                {
                    if (component is Transform)
                    {
                        continue;
                    }

                    var componentGameObject = new GameObject(component.GetType().Name);
                    componentGameObject.transform.parent = ((Transform) target).transform;

                    // Copy the values of the component to the new GameObject
                    EditorUtility.CopySerialized(component, componentGameObject.AddComponent(component.GetType()));

                    // Destroy the component from the original GameObject
                    DestroyImmediate(component);
                }
            }
        }


        private Component[] GetComponents()
        {
            // Get the components attached to the currently selected GameObject
            return ((Transform) target).gameObject.GetComponents<Component>();
        }


        private int ComponentCount()
        {
            return GetComponents().Length;
        }


        private bool IsRoot()
        {
            return ((Transform) target).parent == null;
        }


        protected void GetInternalEditor(string typeName)
        {
            if (InternalEditor != null)
            {
                return;
            }

            var fullName = $"UnityEditor.{typeName}, UnityEditor";
            var editorType = Type.GetType(fullName);

            if (editorType == null)
            {
                Debug.LogWarning($"Could not find the {typeName} type using reflection.");

                return;
            }

            InternalEditor = CreateEditor(target, editorType);
        }
    }
}