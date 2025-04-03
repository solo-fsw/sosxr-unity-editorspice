using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     Based on: Warped Imagination
    ///     https://www.youtube.com/watch?v=iqbUbtwiiz0
    /// </summary>
    [CustomEditor(typeof(AudioSource))]
    public class AudioSourceExtendedEditor : EditorGUIHelpers
    {
        private bool _isPaused;
        private Color _originalColor;


        private void OnEnable()
        {
            _originalColor = GUI.backgroundColor;
            GetInternalEditor("AudioSourceInspector");
        }


        protected override void CustomInspectorContent()
        {
            var targetObject = (AudioSource) target;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (DrawPlayButton(targetObject))
            {
                return;
            }

            if (DrawPauseButton(targetObject))
            {
                return;
            }

            if (DrawStopButton(targetObject))
            {
                return;
            }

            // Restore the original GUI color
            GUI.backgroundColor = _originalColor;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        private bool DrawPlayButton(AudioSource editorTarget)
        {
            GUI.backgroundColor = editorTarget.isPlaying && !_isPaused ? Color.green : _originalColor;

            if (GUILayout.Button("Play", GUILayout.Width(ButtonWidth)))
            {
                if (editorTarget.clip == null)
                {
                    Debug.LogWarning("No audio clip assigned to the AudioSource.");

                    return true;
                }

                editorTarget.Play();
                _isPaused = false;
            }

            return false;
        }


        private bool DrawPauseButton(AudioSource editorTarget)
        {
            GUI.backgroundColor = _isPaused ? Color.yellow : _originalColor;

            if (GUILayout.Button("Pause", GUILayout.Width(ButtonWidth)))
            {
                if (!editorTarget.isPlaying)
                {
                    Debug.LogWarning("AudioSource is not playing.");

                    return true;
                }

                editorTarget.Pause();
                _isPaused = true;
            }

            return false;
        }


        private bool DrawStopButton(AudioSource editorTarget)
        {
            GUI.backgroundColor = !editorTarget.isPlaying && !_isPaused ? _originalColor : Color.red;

            if (GUILayout.Button("Stop", GUILayout.Width(ButtonWidth)))
            {
                if (!editorTarget.isPlaying && !_isPaused)
                {
                    Debug.LogWarning("AudioSource is not playing.");

                    return true;
                }

                editorTarget.Stop();
                _isPaused = false;
            }

            return false;
        }
    }
}