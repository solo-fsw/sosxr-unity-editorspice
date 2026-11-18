using UnityEngine;
using UnityEngine.Events;


namespace SOSXR.EditorSpice.EditorScripts.Samples
{
    /// <summary>
    /// Purpose: Demonstrates UnityEvent declarations with various payload types.
    /// 
    /// Use Case: Learn how to expose UnityEvents in the Inspector and wire callbacks at runtime.
    /// 
    /// How It Works: This sample declares multiple UnityEvent fields with different generic payloads
    /// so you can observe how listeners are added in the Inspector or via code.
    /// 
    /// Integration:Pairs well with EditorSpice event tooling for visualization and learning patterns.
    /// 
    /// Related Classes: ScriptableObjectEventTest, CustomEventTest, UnityEventVisualiser samples.
    /// </summary>
    public class UnityEventDemo : MonoBehaviour
    {
        public UnityEvent OnEvent;
        public UnityEvent<string> OnEventString;
        public UnityEvent<int> OnEventInt;
        public UnityEvent<float> OnEventFloat;
        public UnityEvent<bool> OnEventBool;
        public UnityEvent<Vector2> OnEventVector2;
        public UnityEvent<Vector2Int> OnEventVector2Int;
        public UnityEvent<Vector3> OnEventVector3;
        public UnityEvent<Vector3Int> OnEventVector3Int;
        public UnityEvent<Quaternion> OnEventQuaternion;
        public UnityEvent<GameObject> OnEventGameObject;
        public UnityEvent<Transform> OnEventTransform;
    }
}
