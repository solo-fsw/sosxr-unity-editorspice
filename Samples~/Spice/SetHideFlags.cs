using UnityEngine;


namespace SOSXR
{
    /// <summary>
    /// Purpose: Apply specified HideFlags to a set of objects from the inspector.
    /// 
    /// Use Case: Quickly hide or show objects in the scene view without removing them or changing runtime behavior.
    /// 
    /// How It Works: Iterates over m_objects and assigns m_hideFlags; logs changes for visibility during development.
    /// 
    /// Integration: Demonstrates editor-driven batch updates in EditorSpice samples.
    /// Related Classes: FadeBoolDemo, UnityEventDemo.
    /// </summary>
    public class SetHideFlags : MonoBehaviour
    {
        [SerializeField] private HideFlags m_hideFlags;
        [SerializeField] private Object[] m_objects;


        [ContextMenu(nameof(SetFlags))]
        public void SetFlags()
        {
            foreach (var obj in m_objects)
            {
                obj.hideFlags = m_hideFlags;

                Debug.Log($"Hide-flags of {obj.name} is now {obj.hideFlags}");
            }
        }
    }
}
