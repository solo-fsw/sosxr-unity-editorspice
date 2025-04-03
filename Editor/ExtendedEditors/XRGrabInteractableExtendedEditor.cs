using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


namespace SOSXR.EditorSpice
{
    [CustomEditor(typeof(XRGrabInteractable))]
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