using UnityEngine;
using UnityEngine.Events;


namespace SOSXR.EditorSpice.Samples
{
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