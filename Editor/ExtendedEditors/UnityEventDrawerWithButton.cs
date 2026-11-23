using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;


namespace SOSXR.EditorSpice.EditorScripts
{
    public abstract class UnityEventButtonBase : PropertyDrawer
    {
        protected readonly UnityEventDrawer baseDrawer = new();
        protected const float buttonWidth = 100;
        protected const float fieldWidth = 150;
        protected const string ButtonLabel = "SOSXR: Invoke";
        protected const string DisabledLabel = "Play Mode Only";

        // Add dictionary to store values per property instance
        private static readonly Dictionary<string, object> StoredValues = new();


        protected static string GetPropertyKey(SerializedProperty property)
        {
            if (property?.serializedObject?.targetObject == null)
            {
                return property?.propertyPath ?? "null";
            }

            return $"{property.serializedObject.targetObject.GetEntityId()}_{property.propertyPath}";
        }


        protected static T GetStoredValue<T>(SerializedProperty property, T defaultValue)
        {
            var key = GetPropertyKey(property);

            StoredValues.TryAdd(key, defaultValue);

            return (T) StoredValues[key];
        }


        protected static void SetStoredValue<T>(SerializedProperty property, T value)
        {
            var key = GetPropertyKey(property);
            StoredValues[key] = value;
        }


        protected static Rect FieldRect(Rect position)
        {
            return new Rect(position.xMax - buttonWidth - fieldWidth - 2, position.y + 1, fieldWidth, EditorGUIUtility.singleLineHeight);
        }


        protected static Rect ButtonRect(Rect position)
        {
            return new Rect(position.xMax - buttonWidth - 2, position.y + 1, buttonWidth, EditorGUIUtility.singleLineHeight);
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return baseDrawer.GetPropertyHeight(property, label);
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Prevent drawing when serializedObject or target is null (during domain reload)
            if (property?.serializedObject == null)
            {
                return;
            }

            baseDrawer.OnGUI(position, property, label);

            if (!Application.isPlaying)
            {
                GUI.Label(ButtonRect(position), DisabledLabel);

                return;
            }

            SOSXROnGUI(position, property, label);
        }


        protected abstract void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label);


        /// <summary>
        ///     Returns the event instance behind <paramref name="property" /> on every selected object.
        ///     Follows the property path, so it also works for events inside serializable classes, structs, arrays and lists
        ///     (<c>fieldInfo.GetValue(targetObject)</c> only works for a field declared directly on the component).
        /// </summary>
        protected static IEnumerable<T> GetEvents<T>(SerializedProperty property) where T : class
        {
            foreach (var targetObject in property.serializedObject.targetObjects)
            {
                if (targetObject != null && TryGetValueAtPath(targetObject, property.propertyPath, out var value) && value is T unityEvent)
                {
                    yield return unityEvent;
                }
            }
        }


        /// <summary>Reads the value at a SerializedProperty path such as <c>m_settings.m_events.Array.data[2]</c>.</summary>
        public static bool TryGetValueAtPath(object root, string propertyPath, out object value)
        {
            value = root;

            foreach (var element in propertyPath.Replace(".Array.data[", "[").Split('.'))
            {
                if (value == null)
                {
                    return false;
                }

                var bracket = element.IndexOf('[');

                if (bracket < 0)
                {
                    value = GetFieldValue(value, element);

                    continue;
                }

                var indexText = element.Substring(bracket + 1, element.Length - bracket - 2);

                if (!int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                    || GetFieldValue(value, element.Substring(0, bracket)) is not IList list
                    || index < 0 || index >= list.Count)
                {
                    return false;
                }

                value = list[index];
            }

            return value != null;
        }


        private static object GetFieldValue(object source, string fieldName)
        {
            for (var type = source.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field != null)
                {
                    return field.GetValue(source);
                }
            }

            return null;
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent), true)]
    public class UnityEventDrawerWithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent>(property))
                {
                    unityEvent.Invoke();
                }
            }
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent<string>), true)]
    public class UnityEventDrawerStringWithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            var currentValue = GetStoredValue(property, "SOSXR");
            var newValue = EditorGUI.TextField(FieldRect(position), currentValue);

            if (newValue != currentValue)
            {
                SetStoredValue(property, newValue);
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent<string>>(property))
                {
                    unityEvent.Invoke(newValue);
                }
            }
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent<int>), true)]
    public class UnityEventDrawerIntWithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            var currentValue = GetStoredValue(property, 42);
            var newValue = EditorGUI.IntField(FieldRect(position), currentValue);

            if (newValue != currentValue)
            {
                SetStoredValue(property, newValue);
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent<int>>(property))
                {
                    unityEvent.Invoke(newValue);
                }
            }
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent<float>), true)]
    public class UnityEventDrawerFloatWithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            var currentValue = GetStoredValue(property, 3.14f);
            var newValue = EditorGUI.FloatField(FieldRect(position), currentValue);

            if (!Mathf.Approximately(newValue, currentValue))
            {
                SetStoredValue(property, newValue);
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent<float>>(property))
                {
                    unityEvent.Invoke(newValue);
                }
            }
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent<bool>), true)]
    public class UnityEventDrawerBoolWithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            var currentValue = GetStoredValue(property, false);
            var newValue = EditorGUI.Toggle(FieldRect(position), currentValue);

            if (newValue != currentValue)
            {
                SetStoredValue(property, newValue);
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent<bool>>(property))
                {
                    unityEvent.Invoke(newValue);
                }
            }
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent<Vector2>), true)]
    public class UnityEventDrawerVector2WithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            var currentValue = GetStoredValue(property, new Vector2(0.17f, 1.62f)); // 1.62f – The gravitational acceleration on the Moon (m/s²). 0.17f – The approximate reflectivity (albedo) of the Moon.
            var newValue = EditorGUI.Vector2Field(FieldRect(position), GUIContent.none, currentValue);

            if (newValue != currentValue)
            {
                SetStoredValue(property, newValue);
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent<Vector2>>(property))
                {
                    unityEvent.Invoke(newValue);
                }
            }
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent<Vector3>), true)]
    public class UnityEventDrawerVector3WithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            var currentValue = GetStoredValue(property, new Vector3(1.61f, 3.14f, 9.81f)); // 1.61f – Represents the golden ratio (approximately).  3.14f – Approximates π (pi), used in calculations involving circles. 9.81f – Represents the gravitational acceleration on Earth (m/s²).
            var newValue = EditorGUI.Vector3Field(FieldRect(position), GUIContent.none, currentValue);

            if (newValue != currentValue)
            {
                SetStoredValue(property, newValue);
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent<Vector3>>(property))
                {
                    unityEvent.Invoke(newValue);
                }
            }
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent<Vector2Int>), true)]
    public class UnityEventDrawerVector2IntWithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            var currentValue = GetStoredValue(property, new Vector2Int(0, 1));
            var newValue = EditorGUI.Vector2IntField(FieldRect(position), GUIContent.none, currentValue);

            if (newValue != currentValue)
            {
                SetStoredValue(property, newValue);
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent<Vector2Int>>(property))
                {
                    unityEvent.Invoke(newValue);
                }
            }
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent<Vector3Int>), true)]
    public class UnityEventDrawerVector3IntWithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            var currentValue = GetStoredValue(property, new Vector3Int(32, 64, 128));
            var newValue = EditorGUI.Vector3IntField(FieldRect(position), GUIContent.none, currentValue);

            if (newValue != currentValue)
            {
                SetStoredValue(property, newValue);
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent<Vector3Int>>(property))
                {
                    unityEvent.Invoke(newValue);
                }
            }
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent<Quaternion>), true)]
    public class UnityEventDrawerQuaternionWithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            var currentValue = GetStoredValue(property, new Vector3(45, 90, 180));
            var newValue = EditorGUI.Vector3Field(FieldRect(position), GUIContent.none, currentValue);

            if (newValue != currentValue)
            {
                SetStoredValue(property, newValue);
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent<Quaternion>>(property))
                {
                    unityEvent.Invoke(Quaternion.Euler(newValue));
                }
            }
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent<Transform>), true)]
    public class UnityEventDrawerTransformWithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            var currentValue = GetStoredValue<Transform>(property, null);
            var newValue = EditorGUI.ObjectField(FieldRect(position), GUIContent.none, currentValue, typeof(Transform), true) as Transform;

            if (newValue != currentValue)
            {
                SetStoredValue(property, newValue);
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent<Transform>>(property))
                {
                    unityEvent.Invoke(newValue);
                }
            }
        }
    }


    [CustomPropertyDrawer(typeof(UnityEvent<GameObject>), true)]
    public class UnityEventDrawerGameObjectWithButton : UnityEventButtonBase
    {
        protected override void SOSXROnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null || fieldInfo == null)
            {
                return;
            }

            var currentValue = GetStoredValue<GameObject>(property, null);
            var newValue = EditorGUI.ObjectField(FieldRect(position), GUIContent.none, currentValue, typeof(GameObject), true) as GameObject;

            if (newValue != currentValue)
            {
                SetStoredValue(property, newValue);
            }

            if (GUI.Button(ButtonRect(position), ButtonLabel))
            {
                // Resolve through the property path, so events nested in classes, structs and lists work,
                // and invoke the event on every selected object.
                foreach (var unityEvent in GetEvents<UnityEvent<GameObject>>(property))
                {
                    unityEvent.Invoke(newValue);
                }
            }
        }
    }
}