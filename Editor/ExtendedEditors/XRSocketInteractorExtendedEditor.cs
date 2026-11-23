using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    /// Custom inspector extension for XRSocketInteractor that adds socket interaction buttons.
    /// 
    /// Purpose:
    /// Provides quick access to socket-based interactions for XRSocketInteractor in the Editor.
    /// 
    /// Features:
    /// - Socket interaction buttons
    /// 
    /// How to Extend:
    /// To add this extension to a new component type:
    /// 1. Create a new class inheriting from EditorGUIHelpers
    /// 2. Add [CustomEditor(typeof(YourComponentType))] attribute
    /// 3. Override CustomInspectorContent() to add your UI
    /// 4. Use XRInteractButtons or other helper methods to create buttons/controls
    /// 
    /// Attribution:
    /// None
    /// </summary>
    [CustomEditor(typeof(XRSocketInteractor))]
    [CanEditMultipleObjects]
    public class XRSocketInteractorExtendedEditor : EditorGUIHelpers
    {
        private void OnEnable()
        {
            GetInternalEditor(typeof(XRSocketInteractorEditor));
        }


        protected override void CustomInspectorContent()
        {
            var interactor = (XRSocketInteractor) target;

            XRInteractButtons.CreateHoverButtons(interactor);

            XRInteractButtons.CreateSelectButtons(interactor);
        }
    }
}
