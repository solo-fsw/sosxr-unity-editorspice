using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


namespace SOSXR.EditorSpice
{
    [CustomEditor(typeof(XRGazeInteractor))]
    public class XRGazeInteractorExtendedEditor : EditorGUIHelpers
    {
        private void OnEnable()
        {
            GetInternalEditor(typeof(XRGazeInteractorEditor));
        }


        protected override void CustomInspectorContent()
        {
            var interactor = (XRGazeInteractor) target;

            XRInteractButtons.CreateHoverButtons(interactor);

            XRInteractButtons.CreateSelectButtons(interactor);

            XRInteractButtons.CreateUIHoverButtons(interactor);
        }
    }
}