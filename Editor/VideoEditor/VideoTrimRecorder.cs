using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
#if EDITORSPICE_RECORDER
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
#endif


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Records the trimmed range played by a <see cref="VideoTrimPlayer" /> to a new .mp4 next to the source clip, using the Unity Recorder.
    ///     Recording needs Play Mode and the com.unity.recorder package. Without that package, <see cref="IsAvailable" /> is false
    ///     and <see cref="Record" /> does nothing.
    /// </summary>
    [InitializeOnLoad]
    public static class VideoTrimRecorder
    {
        private const string Extension = ".mp4";

        private static VideoTrimPlayer _player;
#if EDITORSPICE_RECORDER
        private static RecorderController _controller;
#endif


        static VideoTrimRecorder()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }


        /// <summary>
        ///     True when the Unity Recorder package is installed.
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
#if EDITORSPICE_RECORDER
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        ///     True from <see cref="Record" /> until the trimmed range has been recorded or <see cref="Stop" /> is called.
        /// </summary>
        public static bool IsRecording => _player != null;

        /// <summary>
        ///     Full path of the current or last recording, including extension.
        /// </summary>
        public static string OutputPath { get; private set; }


        /// <summary>
        ///     Plays [start, end] of the clip with the player and records it. Returns false when recording cannot start.
        /// </summary>
        public static bool Record(VideoTrimPlayer player, VideoClip clip, double start, double end)
        {
            if (!IsAvailable)
            {
                Debug.LogWarning("Install the Unity Recorder package (com.unity.recorder) to record video.");

                return false;
            }

            if (!Application.isPlaying)
            {
                Debug.LogWarning("Recording only works in Play Mode.");

                return false;
            }

            if (player == null || clip == null)
            {
                Debug.LogWarning("Assign a VideoClip before recording.");

                return false;
            }

            if (IsRecording)
            {
                Debug.LogWarning("Already recording.");

                return false;
            }

            player.Stop();

            if (!player.Load(clip))
            {
                return false;
            }

            var outputPathWithoutExtension = VideoTrimMath.GetOutputPathWithoutExtension(
                GetOutputDirectory(clip),
                clip.name,
                Extension,
                File.Exists,
                DateTime.Now
            );

            OutputPath = outputPathWithoutExtension + Extension;
            _player = player;

#if EDITORSPICE_RECORDER
            CreateController(player, clip, outputPathWithoutExtension);
#endif

            player.PlaybackStarted += OnPlaybackStarted;
            player.PlaybackFinished += OnPlaybackFinished;

            if (player.Play(clip, start, end))
            {
                return true;
            }

            Stop();

            return false;
        }


        /// <summary>
        ///     Stops the recording and the playback. The Recorder finalizes the file.
        /// </summary>
        public static void Stop()
        {
            var player = _player;
            _player = null;

            if (player != null)
            {
                player.PlaybackStarted -= OnPlaybackStarted;
                player.PlaybackFinished -= OnPlaybackFinished;
                player.Stop();
            }

#if EDITORSPICE_RECORDER
            if (_controller != null && _controller.IsRecording())
            {
                _controller.StopRecording();
                Debug.Log($"Recording stopped. Video saved to: {OutputPath}");
                EditorApplication.delayCall += AssetDatabase.Refresh;
            }

            // The settings objects are not destroyed here: the Recorder may still use them while it finalizes the file.
            _controller = null;
#endif
        }


        /// <summary>
        ///     Folder next to the source clip when it lives under Assets, otherwise the Assets folder. Full path.
        /// </summary>
        private static string GetOutputDirectory(VideoClip clip)
        {
            var assetPath = AssetDatabase.GetAssetPath(clip);

            if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets/"))
            {
                return Path.GetDirectoryName(Path.GetFullPath(assetPath));
            }

            return Path.GetFullPath("Assets");
        }


#if EDITORSPICE_RECORDER
        private static void CreateController(VideoTrimPlayer player, VideoClip clip, string outputPathWithoutExtension)
        {
            var renderTexture = player.RenderTexture;

            var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieSettings.name = "VideoEditor Movie Recorder";
            movieSettings.Enabled = true;
            movieSettings.CaptureAudio = true;
            movieSettings.OutputFile = outputPathWithoutExtension;

            movieSettings.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
            };

            movieSettings.ImageInputSettings = new RenderTextureInputSettings
            {
                OutputWidth = renderTexture.width,
                OutputHeight = renderTexture.height,
                RenderTexture = renderTexture
            };

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controllerSettings.AddRecorderSettings(movieSettings);
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
            controllerSettings.FrameRate = (float) clip.frameRate;
            controllerSettings.CapFrameRate = true;

            _controller = new RecorderController(controllerSettings);
        }
#endif


        private static void OnPlaybackStarted()
        {
#if EDITORSPICE_RECORDER
            if (_controller == null || _controller.IsRecording())
            {
                return;
            }

            _controller.PrepareRecording();

            if (_controller.StartRecording())
            {
                Debug.Log($"Recording started: {OutputPath}");
            }
            else
            {
                Debug.LogError("The Recorder could not start. See the Console for details.");
                Stop();
            }
#endif
        }


        private static void OnPlaybackFinished()
        {
            Stop();
        }


        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && IsRecording)
            {
                Stop();
            }
        }
    }
}
