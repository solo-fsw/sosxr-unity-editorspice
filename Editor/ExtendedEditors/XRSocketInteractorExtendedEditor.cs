using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


namespace SOSXR.EditorSpice
{
    [CustomEditor(typeof(XRSocketInteractor))]
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