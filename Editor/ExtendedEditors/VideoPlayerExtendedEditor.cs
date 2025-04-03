using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     Based on: Warped Imagination
    ///     https://www.youtube.com/watch?v=iqbUbtwiiz0
    /// </summary>
    [CustomEditor(typeof(VideoPlayer))]
    public class VideoPlayerExtendedEditor : EditorGUIHelpers
    {
        private bool _isPaused;
        private Color _originalColor;
        private VideoPlayer _targetObject;
        private VideoClip _videoClip;
        private float _startTrim;
        private float _endTrim;
        private float _savedStartTime;
        private float _savedEndTime;
        private float _newDuration;
        private float _currentTime;
        private bool _isMonitoringPlayback = false;

        private bool _isRecording;


        private void OnEnable()
        {
            _originalColor = GUI.backgroundColor;
            GetInternalEditor("VideoPlayerEditor");
        }


        protected override void CustomInspectorContent()
        {
            _targetObject = (VideoPlayer) target;

            if (_targetObject.clip == null)
            {
                return;
            }

            if (_targetObject.clip != _videoClip)
            {
                _videoClip = _targetObject.clip;
                _startTrim = 0;
                _currentTime = 0;
                _endTrim = (float) _videoClip.length;
            }

            if (!Mathf.Approximately(_savedStartTime, _startTrim))
            {
                _targetObject.time = _startTrim;
                _savedStartTime = _startTrim;
                _targetObject.Play();
                _targetObject.Pause();
                _currentTime = 0;
            }

            if (!Mathf.Approximately(_savedEndTime, _endTrim))
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    _targetObject.time = _endTrim;
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    _savedEndTime = _endTrim;
                    _targetObject.time = _startTrim;
                    _targetObject.Play();
                    _targetObject.Pause();
                }

                _currentTime = 0;
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();


            if (DrawPlayButton(_targetObject))
            {
                return;
            }

            if (DrawPauseButton(_targetObject))
            {
                return;
            }

            if (DrawStopButton(_targetObject))
            {
                return;
            }

            GUI.backgroundColor = _originalColor;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawTimeControls();
            DrawControlsSection();

            Repaint();
        }


        private void DrawTimeControls()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            var totalLength = FormatTime((float) _targetObject.clip.length);
            EditorGUILayout.LabelField($"Original Duration: {totalLength}", GUILayout.Width(200));
            EditorGUILayout.LabelField($"Trim Range: {FormatTime(_startTrim)} - {FormatTime(_endTrim)}", GUILayout.Width(200));

            if (_targetObject.isPlaying)
            {
                _currentTime = (float) Math.Round(_targetObject.time - _startTrim, 2, MidpointRounding.AwayFromZero);
            }

            EditorGUILayout.LabelField($"Current Time: {FormatTime(_currentTime)}", GUILayout.Width(200));

            _newDuration = _endTrim - _startTrim;
            EditorGUILayout.LabelField($"New Duration: {FormatTime(_newDuration)}", GUILayout.Width(200));

            EditorGUILayout.EndHorizontal();
        }


        private string FormatTime(float timeInSeconds)
        {
            var minutes = Mathf.FloorToInt(timeInSeconds / 60);
            var seconds = Mathf.FloorToInt(timeInSeconds % 60);
            var milliseconds = Mathf.FloorToInt(timeInSeconds * 1000 % 1000);

            return timeInSeconds >= 60 ? $"{minutes:00}:{seconds:00}.{milliseconds:00}" : $"{seconds:00}.{milliseconds:000}";
        }


        private void DrawControlsSection()
        {
            EditorGUILayout.Space();

            EditorGUILayout.MinMaxSlider("Trim", ref _startTrim, ref _endTrim, 0f, (float) _targetObject.clip.length);
        }


        private void StartMonitoringPlayback()
        {
            if (!_isMonitoringPlayback)
            {
                _isMonitoringPlayback = true;
                EditorApplication.update += MonitorPlaybackTime;
            }
        }


        private void StopMonitoringPlayback()
        {
            if (_isMonitoringPlayback)
            {
                _isMonitoringPlayback = false;
                EditorApplication.update -= MonitorPlaybackTime;
            }
        }


        private void MonitorPlaybackTime()
        {
            if (_targetObject == null)
            {
                return;
            }

            if (!_targetObject.isPlaying)
            {
                return;
            }

            if (_targetObject.time >= _endTrim)
            {
                _targetObject.Stop();
                Debug.Log("Playback stopped as endTrim was reached.");
                StopMonitoringPlayback();
                GoToStart();
            }
        }


        private void GoToStart()
        {
            _currentTime = 0;
            _targetObject.time = _startTrim;
            _targetObject.Play();
            _targetObject.Pause();
        }


        private bool DrawPlayButton(VideoPlayer editorTarget)
        {
            GUI.backgroundColor = editorTarget.isPlaying && !_isPaused ? Color.green : _originalColor;

            if (GUILayout.Button("Play", GUILayout.Width(ButtonWidth)))
            {
                if (editorTarget.clip == null)
                {
                    Debug.LogWarning("No clip assigned to the VideoPlayer.");

                    return true;
                }

                editorTarget.time = _startTrim;
                editorTarget.Play();
                _isPaused = false;
                StartMonitoringPlayback(); // Start checking playback time
            }

            return false;
        }


        private bool DrawPauseButton(VideoPlayer editorTarget)
        {
            GUI.backgroundColor = _isPaused ? Color.yellow : _originalColor;

            if (GUILayout.Button("Pause", GUILayout.Width(ButtonWidth)))
            {
                if (!editorTarget.isPlaying)
                {
                    Debug.LogWarning("VideoPlayer is not playing.");

                    return true;
                }

                editorTarget.Pause();
                _isPaused = true;
            }

            return false;
        }


        private bool DrawStopButton(VideoPlayer editorTarget)
        {
            GUI.backgroundColor = !editorTarget.isPlaying && !_isPaused ? _originalColor : Color.red;

            if (GUILayout.Button("Stop", GUILayout.Width(ButtonWidth)))
            {
                editorTarget.Stop();
                _isPaused = false;

                StopMonitoringPlayback(); // Stop checking playback time
                GoToStart();
            }

            return false;
        }
    }
}