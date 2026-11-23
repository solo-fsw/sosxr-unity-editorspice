using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Debug buttons that raise XR Interaction Toolkit events from the inspector. The events get event args with the
    ///     interactor or interactable filled in (not null), so listeners that read the args do not throw. The other side is not set.
    /// </summary>
    public static class XRInteractButtons
    {
        public const float ButtonWidth = 150f;


        public static void CreateSelectButtons(IXRSelectInteractor interactor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Select Entered", GUILayout.Width(ButtonWidth)))
            {
                interactor.selectEntered?.Invoke(new SelectEnterEventArgs {interactorObject = interactor});
            }

            if (GUILayout.Button("Select Exited", GUILayout.Width(ButtonWidth)))
            {
                interactor.selectExited?.Invoke(new SelectExitEventArgs {interactorObject = interactor});
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        public static void CreateHoverButtons(IXRHoverInteractor interactor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Hover Entered", GUILayout.Width(ButtonWidth)))
            {
                interactor.hoverEntered?.Invoke(new HoverEnterEventArgs {interactorObject = interactor});
            }

            if (GUILayout.Button("Hover Exited", GUILayout.Width(ButtonWidth)))
            {
                interactor.hoverExited?.Invoke(new HoverExitEventArgs {interactorObject = interactor});
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        public static void CreateHoverButtons(XRBaseInteractable interactor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Hover Entered", GUILayout.Width(ButtonWidth)))
            {
                interactor.hoverEntered?.Invoke(new HoverEnterEventArgs {interactableObject = interactor});
            }

            if (GUILayout.Button("Hover Exited", GUILayout.Width(ButtonWidth)))
            {
                interactor.hoverExited?.Invoke(new HoverExitEventArgs {interactableObject = interactor});
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        public static void CreateUIHoverButtons(IUIHoverInteractor interactor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("UI Hover Entered", GUILayout.Width(ButtonWidth)))
            {
                interactor.uiHoverEntered?.Invoke(new UIHoverEventArgs {interactorObject = interactor});
            }

            if (GUILayout.Button("UI Hover Exited", GUILayout.Width(ButtonWidth)))
            {
                interactor.uiHoverExited?.Invoke(new UIHoverEventArgs {interactorObject = interactor});
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        public static void CreateFirstLastHoverButtons(IXRHoverInteractable interactable)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("First Hover Entered", GUILayout.Width(ButtonWidth)))
            {
                interactable.firstHoverEntered?.Invoke(new HoverEnterEventArgs {interactableObject = interactable});
            }

            if (GUILayout.Button("Last Hover Exited", GUILayout.Width(ButtonWidth)))
            {
                interactable.lastHoverExited?.Invoke(new HoverExitEventArgs {interactableObject = interactable});
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        public static void CreateFirstLastSelectButtons(IXRSelectInteractable interactable)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("First Select Entered", GUILayout.Width(ButtonWidth)))
            {
                interactable.firstSelectEntered?.Invoke(new SelectEnterEventArgs {interactableObject = interactable});
            }

            if (GUILayout.Button("Last Select Exited", GUILayout.Width(ButtonWidth)))
            {
                interactable.lastSelectExited?.Invoke(new SelectExitEventArgs {interactableObject = interactable});
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        public static void CreateFirstLastFocusButtons(IXRFocusInteractable interactable)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("First Focus Entered", GUILayout.Width(ButtonWidth)))
            {
                interactable.firstFocusEntered?.Invoke(new FocusEnterEventArgs {interactableObject = interactable});
            }

            if (GUILayout.Button("Last Focus Exited", GUILayout.Width(ButtonWidth)))
            {
                interactable.lastFocusExited?.Invoke(new FocusExitEventArgs {interactableObject = interactable});
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        public static void CreateSelectButtons(IXRSelectInteractable interactable)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Select Entered", GUILayout.Width(ButtonWidth)))
            {
                interactable.selectEntered?.Invoke(new SelectEnterEventArgs {interactableObject = interactable});
            }

            if (GUILayout.Button("Select Exited", GUILayout.Width(ButtonWidth)))
            {
                interactable.selectExited?.Invoke(new SelectExitEventArgs {interactableObject = interactable});
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        public static void CreateFocusButtons(IXRFocusInteractable interactable)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Focus Entered", GUILayout.Width(ButtonWidth)))
            {
                interactable.focusEntered?.Invoke(new FocusEnterEventArgs {interactableObject = interactable});
            }

            if (GUILayout.Button("Focus Exited", GUILayout.Width(ButtonWidth)))
            {
                interactable.focusExited?.Invoke(new FocusExitEventArgs {interactableObject = interactable});
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        public static void CreateActivateButtons(IXRActivateInteractable interactor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Activated", GUILayout.Width(ButtonWidth)))
            {
                interactor.activated?.Invoke(new ActivateEventArgs {interactableObject = interactor});
            }

            if (GUILayout.Button("Deactivated", GUILayout.Width(ButtonWidth)))
            {
                interactor.deactivated?.Invoke(new DeactivateEventArgs {interactableObject = interactor});
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}