using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    /// Custom inspector extension for XRGazeInteractor that adds gaze-based interaction buttons.
    /// 
    /// Purpose:
    /// Facilitates testing and tweaking gaze-driven interactions directly from the Inspector.
    /// 
    /// Features:
    /// - Gaze interaction buttons
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
    [CustomEditor(typeof(XRGazeInteractor))]
    [CanEditMultipleObjects]
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
