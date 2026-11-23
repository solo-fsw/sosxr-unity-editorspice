using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Object = UnityEngine.Object;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Plays an AudioClip from the Project window without opening it.
    ///     Double-click (or Enter) on a clip, or select it and press Space, to play it.
    ///     Repeat on the same clip to stop it. Trigger another clip to switch to it.
    ///     Based on Warped Imagination: https://youtu.be/Gd8M1Ychis8?si=8QNBxDnJG4W1X8Cz
    ///     and https://www.youtube.com/watch?v=D8hOWOK8ByY
    /// </summary>
    [InitializeOnLoad]
    public static class AudioPreviewer
    {
        private const string ProjectBrowserTypeName = "UnityEditor.ProjectBrowser";

        private static Assembly AudioImporterAssembly => typeof(AudioImporter).Assembly;
        private static Type AudioUtil => AudioImporterAssembly.GetType("UnityEditor.AudioUtil");
        private static AudioClip _lastPlayedClip;


        static AudioPreviewer()
        {
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }


        [OnOpenAsset]
#if UNITY_6000_3_OR_NEWER
        public static bool OnOpenAsset(EntityId entityId, int line)
        {
            var obj = EditorUtility.EntityIdToObject(entityId);
#else
        public static bool OnOpenAsset(int entityId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(entityId);
#endif

            if (obj is not AudioClip audioClip)
            {
                return false; // Unity has control of this asset
            }

            TogglePreview(audioClip);

            return true; // I have control of this asset
        }


        /// <summary>
        ///     Called by Unity for every visible Project window item, for every GUI event.
        ///     Handles the Space key once, then marks the event as used.
        /// </summary>
        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            var evt = Event.current;

            if (!IsSpacebarPreviewEvent(evt, EditorGUIUtility.editingTextField, IsProjectWindowFocused()))
            {
                return;
            }

            if (Selection.activeObject is not AudioClip audioClip)
            {
                return;
            }

            TogglePreview(audioClip);
            evt.Use();
        }


        /// <summary>
        ///     True for a plain Space key press (no modifiers) in the focused Project window, while no text field is being edited.
        /// </summary>
        public static bool IsSpacebarPreviewEvent(Event evt, bool editingTextField, bool projectWindowFocused)
        {
            if (evt == null || evt.type != EventType.KeyDown || evt.keyCode != KeyCode.Space)
            {
                return false;
            }

            const EventModifiers blocking = EventModifiers.Shift | EventModifiers.Control | EventModifiers.Alt | EventModifiers.Command;

            if ((evt.modifiers & blocking) != 0)
            {
                return false;
            }

            return projectWindowFocused && !editingTextField;
        }


        /// <summary>
        ///     Decides whether a trigger on <paramref name="clicked" /> should start playback.
        ///     Not playing: play. Playing the same clip: stop only. Playing another clip: switch.
        /// </summary>
        public static bool ShouldPlay(bool isPlaying, Object lastPlayed, Object clicked)
        {
            if (!isPlaying)
            {
                return true;
            }

            return lastPlayed != clicked;
        }


        public static void TogglePreview(AudioClip audioClip)
        {
            var isPlaying = IsPreviewClipPlaying();
            var play = ShouldPlay(isPlaying, _lastPlayedClip, audioClip);

            if (isPlaying)
            {
                StopAllPreviewClips();
            }

            if (play)
            {
                PlayPreviewClip(audioClip);
            }

            _lastPlayedClip = audioClip;
        }


        private static bool IsProjectWindowFocused()
        {
            var focused = EditorWindow.focusedWindow;

            return focused != null && focused.GetType().FullName == ProjectBrowserTypeName;
        }


        public static void PlayPreviewClip(AudioClip audioClip)
        {
            var methodInfo = AudioUtil.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);

            methodInfo?.Invoke(null, new object[] { audioClip, 0, false });
        }


        public static bool IsPreviewClipPlaying()
        {
            var methodInfo = AudioUtil.GetMethod(
                "IsPreviewClipPlaying",
                BindingFlags.Static | BindingFlags.Public);

            return methodInfo != null && (bool)methodInfo.Invoke(null, null);
        }


        public static void StopAllPreviewClips()
        {
            var methodInfo = AudioUtil.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public);

            methodInfo?.Invoke(null, null);
        }
    }
}
