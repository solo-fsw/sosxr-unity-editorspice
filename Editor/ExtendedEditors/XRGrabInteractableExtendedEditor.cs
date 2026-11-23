using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    /// Custom inspector extension for XRGrabInteractable that adds comprehensive interaction buttons.
    /// 
    /// Purpose:
    /// Provides a consolidated set of interaction controls to test common XRGrabInteractable behaviors directly from the Inspector.
    /// 
    /// Features:
    /// - First/Last hover buttons
    /// - Hover buttons
    /// - First/Last select buttons
    /// - Select buttons
    /// - First/Last focus buttons
    /// - Focus buttons
    /// - Activate buttons
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
    [CustomEditor(typeof(XRGrabInteractable))]
    [CanEditMultipleObjects]
    public class XRGrabInteractableExtendedEditor : EditorGUIHelpers
    {
        private void OnEnable()
        {
            GetInternalEditor(typeof(XRGrabInteractableEditor));
        }


        protected override void CustomInspectorContent()
        {
            var interactor = (XRBaseInteractable) target;

            XRInteractButtons.CreateFirstLastHoverButtons(interactor);

            XRInteractButtons.CreateHoverButtons(interactor);

            XRInteractButtons.CreateFirstLastSelectButtons(interactor);

            XRInteractButtons.CreateSelectButtons(interactor);

            XRInteractButtons.CreateFirstLastFocusButtons(interactor);

            XRInteractButtons.CreateFocusButtons(interactor);

            XRInteractButtons.CreateActivateButtons(interactor);
        }
    }
}
