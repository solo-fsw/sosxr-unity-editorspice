using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


namespace SOSXR.EditorSpice
{
    [CustomEditor(typeof(NearFarInteractor))]
    public class XRNearFarInteractorExtendedEditor : EditorGUIHelpers
    {
        private void OnEnable()
        {
            GetInternalEditor(typeof(NearFarInteractorEditor));
        }


        protected override void CustomInspectorContent()
        {
            var interactor = (NearFarInteractor) target;

            XRInteractButtons.CreateHoverButtons(interactor);

            XRInteractButtons.CreateSelectButtons(interactor);

            XRInteractButtons.CreateUIHoverButtons(interactor);
        }
    }
}