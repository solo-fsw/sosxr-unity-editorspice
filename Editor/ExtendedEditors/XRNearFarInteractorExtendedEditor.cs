using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    /// Custom inspector extension for XRNearFarInteractor that adds interaction buttons.
    /// 
    /// Purpose:
    /// Enhances the inspector with quick access to common interactions for Near/Far interactor setups.
    /// 
    /// Features:
    /// - Hover and select interaction buttons
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
    [CustomEditor(typeof(NearFarInteractor))]
    [CanEditMultipleObjects]
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
