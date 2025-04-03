using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    public static class MonoBehaviourUtility
    {
        #if UNITY_EDITOR
        public static int GetMonoBehavioursWithMissingScriptCount(MonoBehaviour behaviour)
        {
            var serializedObject = new SerializedObject(behaviour);
            var prop = serializedObject.FindProperty("m_Script");

            if (prop == null || prop.objectReferenceValue == null)
            {
                return 1;
            }

            return 0;
        }
        #endif
    }
}