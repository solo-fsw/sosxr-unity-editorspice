using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    /// Custom inspector extension for XRDirectInteractor that adds hover and select interaction buttons.
    /// 
    /// Purpose:
    /// Quick access to hover and select actions directly from the inspector for XRDirectInteractor, aiding rapid testing and iteration.
    /// 
    /// Features:
    /// - Hover buttons
    /// - Select buttons
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
    [CustomEditor(typeof(XRDirectInteractor))]
    [CanEditMultipleObjects]
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
