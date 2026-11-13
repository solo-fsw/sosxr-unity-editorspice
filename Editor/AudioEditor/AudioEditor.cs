using System.IO;
using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Editor window to trim, fade and preview an AudioClip, and to save the result as a new .wav next to the source clip.
    ///     The source clip is never changed.
    ///     Based on Simblend: https://github.com/SimblendGames/Simblend-Editor
    /// </summary>
    public class AudioEditor : EditorWindow
    {
        private const int WaveformWidth = 512;
        private const int WaveformHeight = 100;
        private const float TimelineHeight = 20f;
        private const int MaxTimelineMarkers = 200;

        private static readonly Color32 BackgroundColor = new(51, 51, 51, 255);
        private static readonly Color32 WaveformColor = new(255, 128, 0, 255);
        private static readonly Color32 FadedWaveformColor = new(110, 60, 15, 255);
        private static readonly Color TimelineBackgroundColor = new(0.2f, 0.2f, 0.2f);
        private static readonly Color TimelineColor = new(0.7f, 0.7f, 0.7f);
        private static readonly Color PlayheadColor = Color.red;

        [SerializeField] private AudioClip _sourceClip;
        [SerializeField] private float _startTrim;
        [SerializeField] private float _endTrim;
        [SerializeField] private float _fadeIn;
        [SerializeField] private float _fadeOut;
        [SerializeField] private bool _loopPreview;

        private float[] _cachedSamples;
        private int _channels;
        private int _frequency;
        private int _totalFrames;
        private string _loadError;

        private Texture2D _waveformTexture;
        private Color32[] _waveformPixels;
        private bool _waveformDirty;
        private GUIStyle _timelineLabelStyle;
        private Vector2 _scrollPosition;

        private AudioSource _previewSource;
        private AudioClip _previewClip;
        private bool _isPlaying;
        private bool _isDraggingPlayhead;
        private float _playheadTime; // Absolute time in the source clip, in seconds

        /// <summary>
        ///     The clip being edited.
        /// </summary>
        public AudioClip SourceClip => _sourceClip;

        /// <summary>
        ///     True when sample data of the source clip is loaded and can be edited.
        /// </summary>
        public bool HasSamples => _cachedSamples != null;

        /// <summary>
        ///     Error shown when the source clip cannot be read, or null.
        /// </summary>
        public string LoadError => _loadError;

        public float StartTrim => _startTrim;
        public float EndTrim => _endTrim;
        public float FadeIn => _fadeIn;
        public float FadeOut => _fadeOut;

        private float ClipLength => _frequency > 0 ? (float) _totalFrames / _frequency : 0f;
        private float EditedDuration => _endTrim - _startTrim;
        private bool IsPreviewPlaying => _previewSource != null && _previewSource.isPlaying;


        [MenuItem("SOSXR/Audio Editor")]
        public static AudioEditor ShowWindow()
        {
            var window = GetWindow<AudioEditor>("Audio Editor");

            if (Selection.activeObject is AudioClip clip && clip != window._sourceClip)
            {
                window.SetClip(clip);
            }

            return window;
        }


        [MenuItem("Assets/SOSXR/Edit Audio Clip", false, 2000)]
        private static void EditSelectedClip()
        {
            ShowWindow();
        }


        [MenuItem("Assets/SOSXR/Edit Audio Clip", true)]
        private static bool EditSelectedClipValidate()
        {
            return Selection.activeObject is AudioClip;
        }


        /// <summary>
        ///     Loads a clip and resets trim and fades to the full clip without fades.
        /// </summary>
        public void SetClip(AudioClip clip)
        {
            StopPreview();
            _sourceClip = clip;
            LoadClip(false);
        }


        /// <summary>
        ///     Sets trim and fades in seconds. Values are clamped to the clip.
        /// </summary>
        public void SetEdit(float startTrim, float endTrim, float fadeIn, float fadeOut)
        {
            StopPreview();
            _startTrim = startTrim;
            _endTrim = endTrim;
            _fadeIn = fadeIn;
            _fadeOut = fadeOut;
            ClampEdit();
            _waveformDirty = true;
        }


        /// <summary>
        ///     Returns the trimmed and faded interleaved samples, or an empty array when no clip is loaded.
        /// </summary>
        public float[] GetProcessedSamples()
        {
            if (_cachedSamples == null)
            {
                return new float[0];
            }

            return AudioClipProcessor.Process(_cachedSamples, _channels, _frequency, _startTrim, _endTrim, _fadeIn, _fadeOut);
        }


        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;

            if (_sourceClip != null)
            {
                LoadClip(true); // Keep the edit settings after a domain reload
            }
        }


        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopPreview();
            DestroyPreviewSource();
            DestroyWaveformTexture();
        }


        private void OnGUI()
        {
            DrawClipField();

            if (_sourceClip == null)
            {
                EditorGUILayout.HelpBox("Select an AudioClip to edit.", MessageType.Info);

                return;
            }

            if (_loadError != null)
            {
                EditorGUILayout.HelpBox(_loadError, MessageType.Error);

                return;
            }

            if (_cachedSamples == null)
            {
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawWaveformSection();
            DrawTimeInformation();
            DrawControlsSection();
            DrawPreviewControls();
            DrawSaveButton();

            EditorGUILayout.EndScrollView();
        }


        private void OnEditorUpdate()
        {
            if (!_isPlaying)
            {
                return;
            }

            if (IsPreviewPlaying)
            {
                Repaint();

                return;
            }

            // Preview reached the end of the (non-looping) clip
            _isPlaying = false;
            _playheadTime = _startTrim;
            ReleasePreviewClip();
            Repaint();
        }


        private void LoadClip(bool keepEdit)
        {
            _cachedSamples = null;
            _loadError = null;
            _waveformDirty = true;

            if (_sourceClip == null)
            {
                return;
            }

            if (_sourceClip.loadType == AudioClipLoadType.Streaming)
            {
                _loadError = $"'{_sourceClip.name}' uses Load Type 'Streaming'. Its samples cannot be read. Set Load Type to 'Decompress On Load' or 'Compressed In Memory' in the import settings.";

                return;
            }

            if (_sourceClip.loadState != AudioDataLoadState.Loaded)
            {
                _sourceClip.LoadAudioData();
            }

            _channels = _sourceClip.channels;
            _frequency = _sourceClip.frequency;
            _totalFrames = _sourceClip.samples;

            if (_channels <= 0 || _frequency <= 0)
            {
                _loadError = $"'{_sourceClip.name}' has no readable audio data.";

                return;
            }

            var samples = new float[_totalFrames * _channels];

            if (!_sourceClip.GetData(samples, 0))
            {
                _loadError = $"Could not read the samples of '{_sourceClip.name}'. Make sure the clip is imported and 'Load In Background' is off.";

                return;
            }

            _cachedSamples = samples;

            if (!keepEdit)
            {
                _startTrim = 0f;
                _endTrim = ClipLength;
                _fadeIn = 0f;
                _fadeOut = 0f;
            }

            ClampEdit();
            _playheadTime = _startTrim;
        }


        private void ClampEdit()
        {
            AudioClipProcessor.ClampEdit(ClipLength, ref _startTrim, ref _endTrim, ref _fadeIn, ref _fadeOut);
            _playheadTime = Mathf.Clamp(_playheadTime, _startTrim, _endTrim);
        }


        private void DrawClipField()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            var newClip = (AudioClip) EditorGUILayout.ObjectField("Audio Clip", _sourceClip, typeof(AudioClip), false);

            if (EditorGUI.EndChangeCheck())
            {
                SetClip(newClip);
            }

            using (new EditorGUI.DisabledScope(_sourceClip == null))
            {
                if (GUILayout.Button(new GUIContent("Reset", "Reload the clip and reset trim and fades"), GUILayout.Width(60)))
                {
                    SetClip(_sourceClip);
                }
            }

            EditorGUILayout.EndHorizontal();
        }


        private void DrawWaveformSection()
        {
            if (_waveformDirty || _waveformTexture == null)
            {
                RegenerateWaveform();
            }

            EditorGUILayout.Space();

            var waveformRect = GUILayoutUtility.GetRect(WaveformWidth, WaveformHeight, GUILayout.ExpandWidth(true));
            var timelineRect = GUILayoutUtility.GetRect(WaveformWidth, TimelineHeight, GUILayout.ExpandWidth(true));

            GUI.DrawTexture(waveformRect, _waveformTexture, ScaleMode.StretchToFill);
            DrawTimelineMarkers(timelineRect);

            if (_isPlaying && _previewSource != null)
            {
                DrawPlayhead(waveformRect, _startTrim + _previewSource.time);
            }
            else
            {
                HandlePlayheadInteraction(waveformRect);
                DrawPlayhead(waveformRect, _playheadTime);
            }
        }


        private void DrawPlayhead(Rect rect, float time)
        {
            var duration = EditedDuration;
            var normalized = duration > 0f ? Mathf.Clamp01((time - _startTrim) / duration) : 0f;
            var x = Mathf.Lerp(rect.x, rect.xMax, normalized);

            EditorGUI.DrawRect(new Rect(x - 1, rect.y, 2, rect.height), PlayheadColor);

            var labelX = Mathf.Clamp(x - 30, rect.x, rect.xMax - 60);
            GUI.Label(new Rect(labelX, rect.y + 2, 60, 18), AudioClipProcessor.FormatTime(time), EditorStyles.helpBox);
        }


        private void DrawTimelineMarkers(Rect rect)
        {
            EditorGUI.DrawRect(rect, TimelineBackgroundColor);

            var duration = EditedDuration;

            if (duration <= 0f)
            {
                return;
            }

            if (_timelineLabelStyle == null)
            {
                _timelineLabelStyle = new GUIStyle(EditorStyles.miniLabel) {normal = {textColor = TimelineColor}};
            }

            var interval = AudioClipProcessor.CalculateMarkerInterval(duration);
            var firstMarker = Mathf.Ceil(_startTrim / interval);

            for (var i = 0; i < MaxTimelineMarkers; i++)
            {
                var time = (firstMarker + i) * interval;

                if (time > _endTrim + 0.0001f)
                {
                    break;
                }

                var x = Mathf.Lerp(rect.x, rect.xMax, (time - _startTrim) / duration);
                EditorGUI.DrawRect(new Rect(x - 1, rect.y, 2, 5), TimelineColor);
                GUI.Label(new Rect(x - 25, rect.y + 5, 50, 15), AudioClipProcessor.FormatTime(time), _timelineLabelStyle);
            }
        }


        private void HandlePlayheadInteraction(Rect rect)
        {
            var currentEvent = Event.current;

            switch (currentEvent.type)
            {
                case EventType.MouseDown when currentEvent.button == 0 && rect.Contains(currentEvent.mousePosition):
                    _isDraggingPlayhead = true;
                    SeekToMouse(currentEvent.mousePosition.x, rect);
                    currentEvent.Use();

                    break;
                case EventType.MouseDrag when _isDraggingPlayhead:
                    SeekToMouse(currentEvent.mousePosition.x, rect);
                    currentEvent.Use();

                    break;
                case EventType.MouseUp when _isDraggingPlayhead:
                    _isDraggingPlayhead = false;
                    currentEvent.Use();

                    break;
            }
        }


        private void SeekToMouse(float mouseX, Rect rect)
        {
            var normalized = Mathf.Clamp01((mouseX - rect.x) / rect.width);
            _playheadTime = Mathf.Lerp(_startTrim, _endTrim, normalized);
            Repaint();
        }


        private void DrawTimeInformation()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Original Duration", AudioClipProcessor.FormatTime(ClipLength));
            EditorGUILayout.LabelField("Trim Range", $"{AudioClipProcessor.FormatTime(_startTrim)} - {AudioClipProcessor.FormatTime(_endTrim)}");
            EditorGUILayout.LabelField("New Duration", AudioClipProcessor.FormatTime(EditedDuration));
            EditorGUILayout.LabelField("Format", $"{_channels} channel(s), {_frequency} Hz");
        }


        private void DrawControlsSection()
        {
            EditorGUILayout.Space();

            var length = ClipLength;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.MinMaxSlider("Trim", ref _startTrim, ref _endTrim, 0f, length);

            EditorGUILayout.BeginHorizontal();
            _startTrim = EditorGUILayout.FloatField("Start (s)", _startTrim);
            _endTrim = EditorGUILayout.FloatField("End (s)", _endTrim);
            EditorGUILayout.EndHorizontal();

            var duration = Mathf.Max(0f, _endTrim - _startTrim);
            _fadeIn = EditorGUILayout.Slider("Fade In (s)", _fadeIn, 0f, duration);
            _fadeOut = EditorGUILayout.Slider("Fade Out (s)", _fadeOut, 0f, duration);

            if (EditorGUI.EndChangeCheck())
            {
                StopPreview();
                ClampEdit();
                _waveformDirty = true;
            }

            EditorGUI.BeginChangeCheck();
            _loopPreview = EditorGUILayout.Toggle("Loop Preview", _loopPreview);

            if (EditorGUI.EndChangeCheck() && _previewSource != null)
            {
                _previewSource.loop = _loopPreview;
            }
        }


        private void DrawPreviewControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(_isPlaying ? "Pause" : "Play"))
            {
                if (_isPlaying)
                {
                    PausePreview();
                }
                else
                {
                    PlayPreview();
                }
            }

            if (GUILayout.Button("Stop"))
            {
                StopPreview();
            }

            EditorGUILayout.EndHorizontal();
        }


        private void DrawSaveButton()
        {
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(EditedDuration <= 0f))
            {
                if (GUILayout.Button("Save Edited Clip"))
                {
                    SaveEditedClip();
                }
            }
        }


        private void RegenerateWaveform()
        {
            _waveformDirty = false;

            if (_waveformTexture == null)
            {
                _waveformTexture = new Texture2D(WaveformWidth, WaveformHeight, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp
                };

                _waveformPixels = new Color32[WaveformWidth * WaveformHeight];
            }

            for (var i = 0; i < _waveformPixels.Length; i++)
            {
                _waveformPixels[i] = BackgroundColor;
            }

            if (_cachedSamples != null)
            {
                DrawWaveformPixels();
            }

            _waveformTexture.SetPixels32(_waveformPixels);
            _waveformTexture.Apply();
        }


        private void DrawWaveformPixels()
        {
            var startFrame = AudioClipProcessor.TimeToFrame(_startTrim, _frequency, _totalFrames);
            var endFrame = Mathf.Max(startFrame, AudioClipProcessor.TimeToFrame(_endTrim, _frequency, _totalFrames));
            var frameCount = endFrame - startFrame;

            if (frameCount == 0)
            {
                return;
            }

            var peaks = AudioClipProcessor.ComputePeaks(_cachedSamples, _channels, startFrame, endFrame, WaveformWidth);
            var fadeInFrames = AudioClipProcessor.SecondsToFrames(_fadeIn, _frequency, frameCount);
            var fadeOutFrames = AudioClipProcessor.SecondsToFrames(_fadeOut, _frequency, frameCount);
            const int halfHeight = WaveformHeight / 2;

            for (var x = 0; x < WaveformWidth; x++)
            {
                var bucketCenterFrame = (int) (((long) frameCount * x + frameCount / 2) / WaveformWidth);
                var gain = AudioClipProcessor.FadeGain(bucketCenterFrame, frameCount, fadeInFrames, fadeOutFrames);

                DrawColumn(x, Mathf.Clamp01(peaks[x]) * (halfHeight - 1), FadedWaveformColor); // Original amplitude, dimmed
                DrawColumn(x, Mathf.Clamp01(peaks[x] * gain) * (halfHeight - 1), WaveformColor); // Amplitude after fades
            }
        }


        private void DrawColumn(int x, float amplitudeInPixels, Color32 color)
        {
            const int centerY = WaveformHeight / 2;
            var height = Mathf.RoundToInt(amplitudeInPixels);

            for (var y = centerY - height; y <= centerY + height; y++)
            {
                _waveformPixels[y * WaveformWidth + x] = color;
            }
        }


        private void DestroyWaveformTexture()
        {
            if (_waveformTexture != null)
            {
                DestroyImmediate(_waveformTexture);
            }

            _waveformTexture = null;
            _waveformPixels = null;
        }


        private void PlayPreview()
        {
            if (_cachedSamples == null || EditedDuration <= 0f)
            {
                return;
            }

            var samples = GetProcessedSamples();

            if (samples.Length < _channels)
            {
                return;
            }

            EnsurePreviewSource();
            ReleasePreviewClip();

            _previewClip = AudioClip.Create($"{_sourceClip.name}_preview", samples.Length / _channels, _channels, _frequency, false);
            _previewClip.hideFlags = HideFlags.HideAndDontSave;
            _previewClip.SetData(samples, 0);

            var offset = _playheadTime - _startTrim;

            if (offset < 0f || offset >= _previewClip.length - 0.01f)
            {
                offset = 0f; // Restart from the beginning when the playhead sits at the end
            }

            _previewSource.clip = _previewClip;
            _previewSource.loop = _loopPreview;
            _previewSource.time = offset;
            _previewSource.Play();

            _isPlaying = true;
        }


        private void PausePreview()
        {
            if (!_isPlaying || _previewSource == null)
            {
                return;
            }

            _playheadTime = Mathf.Clamp(_startTrim + _previewSource.time, _startTrim, _endTrim);
            _previewSource.Stop();
            _isPlaying = false;
            ReleasePreviewClip();
        }


        private void StopPreview()
        {
            if (_previewSource != null)
            {
                _previewSource.Stop();
            }

            _isPlaying = false;
            _playheadTime = _startTrim;
            ReleasePreviewClip();
        }


        private void EnsurePreviewSource()
        {
            if (_previewSource != null)
            {
                return;
            }

            var previewObject = EditorUtility.CreateGameObjectWithHideFlags("AudioEditorPreview", HideFlags.HideAndDontSave, typeof(AudioSource));
            _previewSource = previewObject.GetComponent<AudioSource>();
            _previewSource.playOnAwake = false;
            _previewSource.spatialBlend = 0f;
        }


        private void ReleasePreviewClip()
        {
            if (_previewSource != null && _previewSource.clip == _previewClip)
            {
                _previewSource.clip = null;
            }

            if (_previewClip != null)
            {
                DestroyImmediate(_previewClip);
            }

            _previewClip = null;
        }


        private void DestroyPreviewSource()
        {
            if (_previewSource != null)
            {
                DestroyImmediate(_previewSource.gameObject);
            }

            _previewSource = null;
        }


        private void SaveEditedClip()
        {
            var sourcePath = AssetDatabase.GetAssetPath(_sourceClip);

            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogError("Could not determine the path of the source clip. Only imported clips can be saved.");

                return;
            }

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{GetSaveFolder(sourcePath)}/{_sourceClip.name}_edited.wav");
            var wavData = WavUtility.FromSamples(GetProcessedSamples(), _channels, _frequency);

            File.WriteAllBytes(Path.GetFullPath(assetPath), wavData);
            AssetDatabase.ImportAsset(assetPath);

            var savedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            if (savedClip != null)
            {
                EditorGUIUtility.PingObject(savedClip);
            }

            Debug.Log($"Saved edited clip to: {assetPath}");
        }


        /// <summary>
        ///     Folder to save the edited clip in: next to the source clip, or the Assets root when the source is not under Assets (e.g. a read-only package).
        /// </summary>
        public static string GetSaveFolder(string sourceAssetPath)
        {
            if (string.IsNullOrEmpty(sourceAssetPath))
            {
                return "Assets";
            }

            var normalized = sourceAssetPath.Replace('\\', '/');

            if (!normalized.StartsWith("Assets/"))
            {
                return "Assets";
            }

            var lastSlash = normalized.LastIndexOf('/');

            return normalized.Substring(0, lastSlash);
        }
    }
}
