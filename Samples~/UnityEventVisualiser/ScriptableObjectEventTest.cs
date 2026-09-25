using UnityEngine;
using UnityEngine.Events;


[CreateAssetMenu(menuName = "SOSXR/UnityEventVisualizer/Scriptable Object Test")]
public class ScriptableObjectEventTest : ScriptableObject
{
    /// <summary>
    /// Purpose: Exposes a UnityEvent that can be invoked from editor tooling or runtime.
    /// 
    /// Use Case: Verify ScriptableObject-backed events can be wired in Inspector and invoked by code/tests.
    /// 
    /// How It Works: Holds a UnityEvent that is invoked via the DoTest method or other editor hooks.
    /// 
    /// Integration: Works with EditorSpice event visualisation/tests.
    /// Related Classes: CustomEventTest, CustomComplexEvent, UnityEventDemo.
    /// </summary>
    public UnityEvent OnTest;


    [ContextMenu(nameof(DoTest))]
    public void DoTest()
    {
        OnTest?.Invoke();
    }
}
