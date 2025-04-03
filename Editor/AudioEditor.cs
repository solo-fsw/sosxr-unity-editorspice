using System;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     Audio clip editor window that supports trimming, fading, and previewing audio files
    ///     Based on Simblend: https://github.com/SimblendGames/Simblend-Editor
    /// </summary>
    public class AudioEditor : EditorWindow
    {
        private static readonly Color DefaultWaveformColor = new(1f, 0.5f, 0f);
        private static readonly Color TimelineColor = new(0.7f, 0.7f, 0.7f);

        // UI State
        private Vector2 scrollPosition;
        private bool isPlaying;
        private bool loopPreview;
        private bool isDraggingPlayhead;
        private float currentPlayTime;

        // Audio State
        private AudioSource previewSource;
        private AudioClip sourceClip;
        private float[] cachedSamples;
        private Texture2D waveformTexture;

        // Edit Parameters
        private float startTrim;
        private float endTrim;
        private float fadeInDuration;
        private float fadeOutDuration;
        private float newDuration;
        private float _startTime;

        // Time display format
        private const int WAVEFORM_WIDTH = 500;
        private const int WAVEFORM_HEIGHT = 100;
        private const float BACKGROUND_DARKNESS = 0.2f;


        private static float endBuffer => 0.02f; // Crude workaround for audio preview loop end. Otherwise it doesn't stop at the end


        [MenuItem("SOSXR/Simblend Audio Editor")]
        public static void ShowWindow()
        {
            GetWindow<AudioEditor>("Audio Editor");
        }


        private void OnEnable()
        {
            InitializeAudioPreview();
        }


        private void OnDestroy()
        {
            CleanupAudioPreview();
        }


        private void OnGUI()
        {
            DrawHeader();

            if (sourceClip == null)
            {
                EditorGUILayout.HelpBox("Please select an audio clip to edit.", MessageType.Info);

                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawWaveformSection();
            DrawControlsSection();
            DrawPreviewControls();
            DrawSaveButton();

            EditorGUILayout.EndScrollView();

            HandlePlaybackUpdate();
        }


        private void SaveEditedClip()
        {
            if (sourceClip == null)
            {
                return;
            }

            var sourceClipPath = AssetDatabase.GetAssetPath(sourceClip);

            if (string.IsNullOrEmpty(sourceClipPath))
            {
                Debug.LogError("Could not determine the path of the source clip.");

                return;
            }

            var directory = Path.GetDirectoryName(sourceClipPath);

            if (string.IsNullOrEmpty(directory))
            {
                Debug.LogError("Could not determine the directory of the source clip.");

                return;
            }

            var savePath = Path.Combine(directory, $"{sourceClip.name}_edited.wav");

            var processedClip = CreatePreviewClip();
            var wavData = WavUtility.FromAudioClip(processedClip);
            File.WriteAllBytes(savePath, wavData);

            DestroyImmediate(processedClip);
            AssetDatabase.Refresh();

            Debug.Log($"Saved edited clip to: {savePath}");
        }


        private void InitializeAudioPreview()
        {
            if (previewSource != null)
            {
                return;
            }

            var previewObject = new GameObject("AudioPreview");
            previewSource = previewObject.AddComponent<AudioSource>();
            previewSource.hideFlags = HideFlags.HideAndDontSave;
        }


        private void CleanupAudioPreview()
        {
            if (previewSource == null)
            {
                return;
            }

            StopPreview();
            DestroyImmediate(previewSource.gameObject);
        }


        private void DrawHeader()
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var newClip = (AudioClip) EditorGUILayout.ObjectField("Audio Clip", sourceClip, typeof(AudioClip), false);

            if (EditorGUI.EndChangeCheck())
            {
                HandleClipChange(newClip);
            }

            if (GUILayout.Button("Reinitialize", GUILayout.Width(75)))
            {
                HandleClipChange();
            }

            EditorGUILayout.EndHorizontal();
        }


        private void DrawWaveformSection()
        {
            if (waveformTexture == null)
            {
                return;
            }

            DrawWaveformPreview();
            DrawTimeInformation();
        }


        private void DrawWaveformPreview()
        {
            EditorGUILayout.LabelField(""); // Empty space for alignment
            var rect = GUILayoutUtility.GetRect(WAVEFORM_WIDTH, WAVEFORM_HEIGHT);
            GUI.DrawTexture(rect, waveformTexture);

            DrawTimelineMarkers(rect);

            if (previewSource.isPlaying)
            {
                DrawPlayhead(rect, previewSource.time + startTrim);
                Repaint();
            }
            else
            {
                HandlePlayheadInteraction(rect);
                DrawPlayhead(rect, currentPlayTime);
            }
        }


        private void DrawPlayhead(Rect rect, float time)
        {
            var playheadRect = GetPlayheadRect(rect, time);
            EditorGUI.DrawRect(playheadRect, Color.red);

            var currentTimeLabel = new Rect(playheadRect.x - 25, rect.y - 20, 60, 18);
            GUI.Label(currentTimeLabel, FormatTime(time), EditorStyles.helpBox);
        }


        private void DrawTimelineMarkers(Rect rect)
        {
            var timelineRect = new Rect(rect.x, rect.y + rect.height, rect.width, 20);
            EditorGUI.DrawRect(timelineRect, new Color(0.2f, 0.2f, 0.2f));

            var duration = endTrim - startTrim;
            var markerInterval = CalculateMarkerInterval(duration);
            var markerCount = Mathf.CeilToInt(duration / markerInterval);

            for (var i = 0; i <= markerCount; i++)
            {
                var time = i * markerInterval;

                if (time > endTrim)
                {
                    break;
                }

                var x = Mathf.Lerp(rect.x, rect.x + rect.width, time / duration);

                // Draw marker line
                EditorGUI.DrawRect(new Rect(x - 1, timelineRect.y, 2, 5), TimelineColor);

                // Draw time label
                var timeStr = FormatTime(time);

                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = {textColor = TimelineColor}
                };

                GUI.Label(new Rect(x - 20, timelineRect.y + 5, 40, 15), timeStr, style);
            }
        }


        private void HandlePlayheadInteraction(Rect rect)
        {
            if (isPlaying)
            {
                return; // Don't handle mouse interaction while playing
            }

            var playheadRect = GetPlayheadRect(rect, currentPlayTime);
            var mouseEvent = Event.current;

            if ((mouseEvent.type == EventType.MouseDown && rect.Contains(mouseEvent.mousePosition)) || (mouseEvent.type == EventType.MouseDown && playheadRect.Contains(mouseEvent.mousePosition)))
            {
                isDraggingPlayhead = true;
                UpdatePlayheadPosition(mouseEvent.mousePosition.x, rect);
                mouseEvent.Use();
            }
            else if (mouseEvent.type == EventType.MouseDrag && isDraggingPlayhead)
            {
                UpdatePlayheadPosition(mouseEvent.mousePosition.x, rect);
                mouseEvent.Use();
            }
            else if (mouseEvent.type == EventType.MouseUp)
            {
                isDraggingPlayhead = false;
            }
        }


        private void UpdatePlayheadPosition(float mouseX, Rect rect)
        {
            var normalizedPosition = Mathf.Clamp01((mouseX - rect.x) / rect.width);
            var newTime = Mathf.Lerp(startTrim, endTrim, normalizedPosition);
            SeekToTime(newTime);
        }


        private Rect GetPlayheadRect(Rect waveformRect, float time)
        {
            var normalizedTime = Mathf.Clamp01((time - startTrim) / (endTrim - startTrim));
            var x = Mathf.Lerp(waveformRect.x, waveformRect.xMax, normalizedTime);

            return new Rect(x - 1, waveformRect.y, 2, waveformRect.height + 20);
        }


        private void HandlePlaybackUpdate()
        {
            if (!isPlaying || previewSource == null)
            {
                return;
            }

            currentPlayTime = startTrim + previewSource.time;

            if (!loopPreview && currentPlayTime >= endTrim - endBuffer)
            {
                StopPreview();
            }

            Repaint();
        }


        private void SeekToTime(float time)
        {
            time = Mathf.Clamp(time, 0, sourceClip.length);
            currentPlayTime = time;

            if (previewSource != null && previewSource.clip != null)
            {
                previewSource.time = time - startTrim;

                if (!isPlaying)
                {
                    previewSource.Play();
                    previewSource.Pause();
                }
            }

            Repaint();
        }


        private float CalculateMarkerInterval(float duration)
        {
            if (duration <= 1f)
            {
                return 0.1f;
            }

            if (duration <= 5f)
            {
                return 0.5f;
            }

            if (duration <= 10f)
            {
                return 1f;
            }

            if (duration <= 30f)
            {
                return 5f;
            }

            return duration <= 60f ? 10f : 30f;
        }


        private string FormatTime(float timeInSeconds)
        {
            var minutes = Mathf.FloorToInt(timeInSeconds / 60);
            var seconds = Mathf.FloorToInt(timeInSeconds % 60);
            var milliseconds = Mathf.FloorToInt(timeInSeconds * 1000 % 1000);

            return timeInSeconds >= 60 ? $"{minutes:00}:{seconds:00}.{milliseconds:00}" : $"{seconds:00}.{milliseconds:000}";
        }


        private void DrawTimeInformation()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(200)); // Empty space for alignment
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            var totalLength = FormatTime(sourceClip.length);
            EditorGUILayout.LabelField($"Original Duration: {totalLength}", GUILayout.Width(200));

            EditorGUILayout.LabelField($"Trim Range: {FormatTime(startTrim)} - {FormatTime(endTrim)}", GUILayout.Width(200));

            EditorGUILayout.LabelField($"Fade In: {FormatTime(fadeInDuration)} - Fade Out: {FormatTime(endTrim - fadeOutDuration)}", GUILayout.Width(250));

            newDuration = endTrim - startTrim;
            EditorGUILayout.LabelField($"New Duration: {FormatTime(newDuration)}", GUILayout.Width(200));

            EditorGUILayout.EndHorizontal();
        }


        private void DrawControlsSection()
        {
            EditorGUILayout.Space();

            EditorGUILayout.MinMaxSlider("Trim", ref startTrim, ref endTrim, 0f, sourceClip.length);
            EditorGUILayout.MinMaxSlider("Fade", ref fadeInDuration, ref fadeOutDuration, 0f, endTrim);

            loopPreview = EditorGUILayout.Toggle("Loop Preview", loopPreview);

            if (GUI.changed)
            {
                RegenerateWaveform();
            }
        }


        private void DrawPreviewControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(isPlaying ? "Pause" : "Play"))
            {
                TogglePreview();
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

            if (GUILayout.Button("Save Edited Clip"))
            {
                SaveEditedClip();
            }
        }


        private void HandleClipChange(AudioClip newClip = null)
        {
            StopPreview();

            if (newClip != null)
            {
                sourceClip = newClip;
            }

            startTrim = 0;
            fadeInDuration = 0;
            currentPlayTime = 0;

            RegenerateWaveform();

            if (sourceClip == null)
            {
                return;
            }

            endTrim = sourceClip.length;
            fadeOutDuration = sourceClip.length;
            cachedSamples = new float[sourceClip.samples * sourceClip.channels];
            sourceClip.GetData(cachedSamples, 0);
        }


        private void RegenerateWaveform()
        {
            if (sourceClip == null || cachedSamples == null)
            {
                return;
            }

            waveformTexture = GenerateWaveformTexture(
                cachedSamples,
                sourceClip.channels,
                sourceClip.frequency,
                startTrim,
                endTrim,
                fadeInDuration,
                endTrim - fadeOutDuration
            );
        }


        private Texture2D GenerateWaveformTexture(float[] samples, int channels, int frequency, float start, float end, float fadeIn, float fadeOut)
        {
            var texture = new Texture2D(WAVEFORM_WIDTH, WAVEFORM_HEIGHT);
            var colors = new Color[WAVEFORM_WIDTH * WAVEFORM_HEIGHT];
            Array.Fill(colors, new Color(BACKGROUND_DARKNESS, BACKGROUND_DARKNESS, BACKGROUND_DARKNESS));

            var startSample = Mathf.FloorToInt(start * frequency * channels);
            var endSample = Mathf.FloorToInt(end * frequency * channels);
            var samplesPerPixel = (endSample - startSample) / WAVEFORM_WIDTH;

            DrawWaveformLines(colors, samples, startSample, endSample, samplesPerPixel, frequency, channels, fadeIn, fadeOut);

            texture.SetPixels(colors);
            texture.Apply();

            return texture;
        }


        private void DrawWaveformLines(Color[] colors, float[] samples, int startSample, int endSample, int samplesPerPixel, int frequency, int channels, float fadeIn, float fadeOut)
        {
            var fadeInSamples = Mathf.FloorToInt(fadeIn * frequency * channels);
            var fadeOutSamples = Mathf.FloorToInt(fadeOut * frequency * channels);

            for (var x = 0; x < WAVEFORM_WIDTH; x++)
            {
                var sampleOffset = startSample + x * samplesPerPixel;
                var peakValue = CalculatePeakValue(samples, sampleOffset, samplesPerPixel);

                var currentPosition = sampleOffset - startSample;

                if (currentPosition < fadeInSamples)
                {
                    peakValue *= (float) currentPosition / fadeInSamples;
                }
                else if (currentPosition > endSample - startSample - fadeOutSamples)
                {
                    peakValue *= (float) (endSample - startSample - currentPosition) / fadeOutSamples;
                }

                DrawWaveformLine(colors, x, peakValue);
            }
        }


        private float CalculatePeakValue(float[] samples, int offset, int length)
        {
            var peak = 0f;
            var end = Math.Min(offset + length, samples.Length);

            for (var i = offset; i < end; i++)
            {
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }

            return peak;
        }


        private void DrawWaveformLine(Color[] colors, int x, float amplitude)
        {
            var heightScale = WAVEFORM_HEIGHT / 2;
            var heightPosition = Mathf.FloorToInt(amplitude * heightScale);
            var centerY = WAVEFORM_HEIGHT / 2;

            for (var y = 0; y < heightPosition; y++)
            {
                var topIndex = (centerY + y) * WAVEFORM_WIDTH + x;
                var bottomIndex = (centerY - y) * WAVEFORM_WIDTH + x;

                if (topIndex < colors.Length && bottomIndex >= 0)
                {
                    colors[topIndex] = DefaultWaveformColor;
                    colors[bottomIndex] = DefaultWaveformColor;
                }
            }
        }


        private void TogglePreview()
        {
            if (isPlaying)
            {
                previewSource.Pause();
                isPlaying = false;
            }
            else
            {
                PlayPreview();
            }
        }


        private void PlayPreview()
        {
            if (sourceClip == null)
            {
                return;
            }

            var previewClip = CreatePreviewClip();
            previewSource.clip = previewClip;
            previewSource.loop = loopPreview;

            // Use the current playhead position relative to the trim start
            _startTime = currentPlayTime;
            var relativeTime = currentPlayTime - startTrim;
            previewSource.time = Mathf.Clamp(relativeTime, 0f, previewClip.length);
            previewSource.Play();

            isPlaying = true;
        }


        private void StopPreview()
        {
            if (previewSource == null)
            {
                return;
            }

            previewSource.Stop();

            if (previewSource.clip != null && previewSource.clip != sourceClip)
            {
                DestroyImmediate(previewSource.clip);
            }

            isPlaying = false;
            currentPlayTime = _startTime;
            Repaint();
        }


        private AudioClip CreatePreviewClip()
        {
            var trimmedSamples = ProcessAudioData(cachedSamples, sourceClip.channels, sourceClip.frequency);

            var previewClip = AudioClip.Create("Preview", trimmedSamples.Length / sourceClip.channels, sourceClip.channels, sourceClip.frequency, false);

            previewClip.SetData(trimmedSamples, 0);

            return previewClip;
        }


        private float[] ProcessAudioData(float[] samples, int channels, int frequency)
        {
            var startSample = Mathf.FloorToInt(startTrim * frequency * channels);
            var endSample = Mathf.FloorToInt(endTrim * frequency * channels);
            var length = endSample - startSample;

            var processed = new float[length];
            Array.Copy(samples, startSample, processed, 0, length);

            ApplyFade(processed, frequency * channels, fadeInDuration, endTrim - fadeOutDuration);

            return processed;
        }


        private void ApplyFade(float[] samples, int samplesPerSecond, float fadeInTime, float fadeOutTime)
        {
            var fadeInSamples = Mathf.FloorToInt(fadeInTime * samplesPerSecond);
            var fadeOutSamples = Mathf.FloorToInt(fadeOutTime * samplesPerSecond);

            // Fade in
            for (var i = 0; i < fadeInSamples && i < samples.Length; i++)
            {
                samples[i] *= (float) i / fadeInSamples;
            }

            // Fade out
            for (var i = 0; i < fadeOutSamples && i < samples.Length; i++)
            {
                var index = samples.Length - 1 - i;
                samples[index] *= (float) i / fadeOutSamples;
            }
        }
    }
}