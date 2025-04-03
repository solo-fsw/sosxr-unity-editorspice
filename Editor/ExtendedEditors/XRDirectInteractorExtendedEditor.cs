using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


namespace SOSXR.EditorSpice
{
    [CustomEditor(typeof(XRDirectInteractor))]
    public class XRInteractorExtendedBaseInteractorEditor : EditorGUIHelpers
    {
        private void OnEnable()
        {
            GetInternalEditor(typeof(XRDirectInteractorEditor));
        }


        protected override void CustomInspectorContent()
        {
            var interactor = (XRDirectInteractor) target;

            XRInteractButtons.CreateHoverButtons(interactor);

            XRInteractButtons.CreateSelectButtons(interactor);
        }
    }
}