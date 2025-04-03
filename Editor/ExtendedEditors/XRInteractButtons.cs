using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;


namespace SOSXR.EditorSpice
{
    public static class XRInteractButtons
    {
        public const float ButtonWidth = 150f;


        public static void CreateSelectButtons(IXRSelectInteractor interactor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Select Entered", GUILayout.Width(ButtonWidth)))
            {
                interactor.selectEntered?.Invoke(null);
            }

            if (GUILayout.Button("Select Exited", GUILayout.Width(ButtonWidth)))
            {
                interactor.selectExited?.Invoke(null);
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
                interactor.hoverEntered?.Invoke(null);
            }

            if (GUILayout.Button("Hover Exited", GUILayout.Width(ButtonWidth)))
            {
                interactor.hoverExited?.Invoke(null);
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
                interactor.hoverEntered?.Invoke(null);
            }

            if (GUILayout.Button("Hover Exited", GUILayout.Width(ButtonWidth)))
            {
                interactor.hoverExited?.Invoke(null);
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
                interactor.uiHoverEntered?.Invoke(null);
            }

            if (GUILayout.Button("UI Hover Exited", GUILayout.Width(ButtonWidth)))
            {
                interactor.uiHoverExited?.Invoke(null);
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
                interactable.firstHoverEntered?.Invoke(null);
            }

            if (GUILayout.Button("Last Hover Exited", GUILayout.Width(ButtonWidth)))
            {
                interactable.lastHoverExited?.Invoke(null);
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
                interactable.firstSelectEntered?.Invoke(null);
            }

            if (GUILayout.Button("Last Select Exited", GUILayout.Width(ButtonWidth)))
            {
                interactable.lastSelectExited?.Invoke(null);
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
                interactable.firstFocusEntered?.Invoke(null);
            }

            if (GUILayout.Button("Last Focus Exited", GUILayout.Width(ButtonWidth)))
            {
                interactable.lastFocusExited?.Invoke(null);
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
                interactable.selectEntered?.Invoke(null);
            }

            if (GUILayout.Button("Select Exited", GUILayout.Width(ButtonWidth)))
            {
                interactable.selectExited?.Invoke(null);
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
                interactable.focusEntered?.Invoke(null);
            }

            if (GUILayout.Button("Focus Exited", GUILayout.Width(ButtonWidth)))
            {
                interactable.focusExited?.Invoke(null);
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
                interactor.activated?.Invoke(null);
            }

            if (GUILayout.Button("Deactivated", GUILayout.Width(ButtonWidth)))
            {
                interactor.deactivated?.Invoke(null);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}