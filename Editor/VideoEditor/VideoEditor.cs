using UnityEditor;
using UnityEngine;
using UnityEngine.Video;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Editor window to preview a trimmed range of a VideoClip and record it to a new .mp4 next to the source clip.
    ///     Preview and recording work in Play Mode only. Recording needs the Unity Recorder package (com.unity.recorder).
    ///     The source clip is never changed.
    /// </summary>
    public class VideoEditor : EditorWindow
    {
        private const float MaxPreviewHeight = 360f;

        [SerializeField] private VideoClip _clip;
        [SerializeField] private float _startTrim;
        [SerializeField] private float _endTrim;

        private VideoTrimPlayer _player;
        private Vector2 _scrollPosition;

        /// <summary>
        ///     The clip being edited.
        /// </summary>
        public VideoClip Clip => _clip;

        /// <summary>
        ///     Player used for preview and recording. Null until Play or Record is used in Play Mode.
        /// </summary>
        public VideoTrimPlayer Player => _player;

        /// <summary>
        ///     Trim start in seconds, clamped to the clip.
        /// </summary>
        public float StartTrim
        {
            get
            {
                ClampedTrim(out var start, out _);

                return (float) start;
            }
        }

        /// <summary>
        ///     Trim end in seconds, clamped to the clip. An end that is not after the start means: until the end of the clip.
        /// </summary>
        public float EndTrim
        {
            get
            {
                ClampedTrim(out _, out var end);

                return (float) end;
            }
        }

        private bool IsRecordingThisWindow => VideoTrimRecorder.IsRecording && _player != null && _player.IsPlaying;


        [MenuItem("SOSXR/Video Editor")]
        public static VideoEditor ShowWindow()
        {
            var window = GetWindow<VideoEditor>("Video Editor");

            if (Selection.activeObject is VideoClip clip && clip != window._clip)
            {
                window.SetClip(clip);
            }

            return window;
        }


        [MenuItem("Assets/SOSXR/Edit Video Clip", false, 2001)]
        private static void EditSelectedClip()
        {
            ShowWindow();
        }


        [MenuItem("Assets/SOSXR/Edit Video Clip", true)]
        private static bool EditSelectedClipValidate()
        {
            return Selection.activeObject is VideoClip;
        }


        /// <summary>
        ///     Sets the clip and selects the whole clip. Stops playback.
        /// </summary>
        public void SetClip(VideoClip clip)
        {
            StopPlayback();
            _clip = clip;
            _startTrim = 0f;
            _endTrim = clip != null ? (float) clip.length : 0f;
        }


        /// <summary>
        ///     Sets the trim range in seconds. Takes effect on the next Play or Record.
        /// </summary>
        public void SetTrim(float startTrim, float endTrim)
        {
            _startTrim = startTrim;
            _endTrim = endTrim;
            ClampedTrim(out var start, out var end);
            _startTrim = (float) start;
            _endTrim = (float) end;
        }


        /// <summary>
        ///     Plays the trimmed range, or resumes when paused. Play Mode only.
        /// </summary>
        public bool Play()
        {
            if (_player != null && _player.IsPaused)
            {
                return _player.Resume();
            }

            if (_clip == null)
            {
                Debug.LogWarning("VideoEditor: no clip assigned.");

                return false;
            }

            return EnsurePlayer().Play(_clip, StartTrim, EndTrim);
        }


        /// <summary>
        ///     Plays and records the trimmed range. Play Mode and the Unity Recorder package only.
        /// </summary>
        public bool Record()
        {
            return VideoTrimRecorder.Record(EnsurePlayer(), _clip, StartTrim, EndTrim);
        }


        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }


        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            DisposePlayer();
        }


        private void OnEditorUpdate()
        {
            if (_player == null)
            {
                return;
            }

            _player.Tick();

            if (_player.IsPlaying)
            {
                Repaint();
            }
        }


        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                DisposePlayer(); // The temporary GameObject is DontSave, so it would outlive Play Mode
            }

            Repaint();
        }


        private VideoTrimPlayer EnsurePlayer()
        {
            return _player ??= new VideoTrimPlayer();
        }


        private void StopPlayback()
        {
            if (IsRecordingThisWindow)
            {
                VideoTrimRecorder.Stop();
            }

            _player?.Stop();
        }


        private void DisposePlayer()
        {
            if (_player == null)
            {
                return;
            }

            StopPlayback();
            _player.Dispose();
            _player = null;
        }


        private void ClampedTrim(out double start, out double end)
        {
            var length = _clip != null ? _clip.length : 0d;
            VideoTrimMath.ClampTrim(length, _startTrim, _endTrim, out start, out end);
        }


        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawClipField();

            if (_clip == null)
            {
                EditorGUILayout.HelpBox("Select a VideoClip to edit.", MessageType.Info);
                EditorGUILayout.EndScrollView();

                return;
            }

            DrawTrim();
            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to preview and record the trimmed range.", MessageType.Info);
            }
            else
            {
                DrawPreview();
                DrawPlaybackControls();
                EditorGUILayout.Space();
                DrawRecordControls();
            }

            EditorGUILayout.EndScrollView();
        }


        private void DrawClipField()
        {
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(VideoTrimRecorder.IsRecording))
            {
                EditorGUI.BeginChangeCheck();
                var newClip = (VideoClip) EditorGUILayout.ObjectField("Video Clip", _clip, typeof(VideoClip), false);

                if (EditorGUI.EndChangeCheck())
                {
                    SetClip(newClip);
                }
            }
        }


        private void DrawTrim()
        {
            var length = (float) _clip.length;
            var start = StartTrim;
            var end = EndTrim;

            using (new EditorGUI.DisabledScope(VideoTrimRecorder.IsRecording))
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.MinMaxSlider("Trim", ref start, ref end, 0f, length);
                EditorGUILayout.BeginHorizontal();
                start = EditorGUILayout.FloatField("Start (s)", start);
                end = EditorGUILayout.FloatField("End (s)", end);
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    SetTrim(start, end);
                }
            }

            var frameRate = _clip.frameRate;
            var startFrame = VideoTrimMath.TimeToFrame(StartTrim, frameRate);
            var endFrame = VideoTrimMath.TimeToFrame(EndTrim, frameRate);

            EditorGUILayout.LabelField("Clip", $"{length:0.000} s, {_clip.width}x{_clip.height}, {frameRate:0.##} fps");
            EditorGUILayout.LabelField("Trimmed", $"{EndTrim - StartTrim:0.000} s (frames {startFrame} - {endFrame})");
        }


        private void DrawPreview()
        {
            var texture = _player?.RenderTexture;

            if (texture == null)
            {
                return;
            }

            var aspect = (float) texture.width / texture.height;
            var rect = GUILayoutUtility.GetAspectRect(aspect, GUILayout.MaxHeight(MaxPreviewHeight));
            EditorGUI.DrawRect(rect, Color.black);
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, false);
        }


        private void DrawPlaybackControls()
        {
            var isPlaying = _player != null && _player.IsPlaying;
            var isRunning = isPlaying && !_player.IsPaused;

            using (new EditorGUI.DisabledScope(VideoTrimRecorder.IsRecording))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(isRunning ? "Pause" : "Play"))
                {
                    if (isRunning)
                    {
                        _player.Pause();
                    }
                    else
                    {
                        Play();
                    }
                }

                using (new EditorGUI.DisabledScope(!isPlaying))
                {
                    if (GUILayout.Button("Stop"))
                    {
                        _player.Stop();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            if (isPlaying)
            {
                EditorGUILayout.LabelField("Time", $"{_player.CurrentTime:0.000} s / {_player.EndTime:0.000} s");
            }
        }


        private void DrawRecordControls()
        {
            if (!VideoTrimRecorder.IsAvailable)
            {
                EditorGUILayout.HelpBox("Install the Unity Recorder package (com.unity.recorder 5.0.0 or newer) to record the trimmed range to a new .mp4.", MessageType.Info);

                return;
            }

            if (VideoTrimRecorder.IsRecording)
            {
                if (GUILayout.Button("Stop Recording"))
                {
                    VideoTrimRecorder.Stop();
                }

                EditorGUILayout.HelpBox($"Recording to {VideoTrimRecorder.OutputPath}", MessageType.None);

                return;
            }

            using (new EditorGUI.DisabledScope(_player != null && _player.IsPlaying))
            {
                if (GUILayout.Button("Record Trimmed Range"))
                {
                    Record();
                }
            }

            if (!string.IsNullOrEmpty(VideoTrimRecorder.OutputPath))
            {
                EditorGUILayout.HelpBox($"Last recording: {VideoTrimRecorder.OutputPath}", MessageType.None);
            }
        }
    }
}
