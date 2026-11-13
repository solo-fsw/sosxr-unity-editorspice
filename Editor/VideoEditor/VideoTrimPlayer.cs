using System;
using UnityEngine;
using UnityEngine.Video;
using Object = UnityEngine.Object;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Plays a trimmed range of a VideoClip into a RenderTexture. Used by the <see cref="VideoEditor" /> window.
    ///     Works in Play Mode only. Creates a temporary, unsaved GameObject with a VideoPlayer and an AudioSource.
    ///     Call <see cref="Tick" /> every editor update and <see cref="Dispose" /> before leaving Play Mode.
    /// </summary>
    public sealed class VideoTrimPlayer : IDisposable
    {
        private const string RootName = "VideoEditor (temporary)";

        private GameObject _root;
        private VideoPlayer _videoPlayer;
        private AudioSource _audioSource;
        private RenderTexture _renderTexture;
        private VideoClip _clip;
        private double _startTime;
        private double _endTime;
        private bool _isPlaying;
        private bool _isPaused;
        private bool _isWaitingForSeek;

        /// <summary>
        ///     Raised when playback of the trimmed range starts, after the player has seeked to the start time.
        /// </summary>
        public event Action PlaybackStarted;

        /// <summary>
        ///     Raised when playback of the trimmed range ends: at the end time, at the end of the clip, or on Stop.
        /// </summary>
        public event Action PlaybackFinished;

        public VideoClip Clip => _clip;

        /// <summary>
        ///     RenderTexture the video plays into. Null until a clip is loaded.
        /// </summary>
        public RenderTexture RenderTexture => _renderTexture;

        /// <summary>
        ///     True from <see cref="Play" /> until the range ends or <see cref="Stop" /> is called, also while paused.
        /// </summary>
        public bool IsPlaying => _isPlaying;

        public bool IsPaused => _isPaused;
        public double StartTime => _startTime;
        public double EndTime => _endTime;
        public double CurrentTime => _videoPlayer != null ? _videoPlayer.time : 0d;


        /// <summary>
        ///     Assigns the clip to the player and creates a matching RenderTexture. Stops playback of a previous clip.
        /// </summary>
        public bool Load(VideoClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("VideoEditor: no clip assigned.");

                return false;
            }

            if (!Application.isPlaying)
            {
                Debug.LogWarning("VideoEditor: preview and recording only work in Play Mode.");

                return false;
            }

            EnsureRig();

            if (_clip == clip && _renderTexture != null)
            {
                return true;
            }

            Stop();
            _clip = clip;
            _videoPlayer.clip = clip;
            CreateRenderTexture();

            return true;
        }


        /// <summary>
        ///     Plays [start, end] of the clip from the start. Values are clamped to the clip.
        ///     An end that is not after the start means: until the end of the clip.
        /// </summary>
        public bool Play(VideoClip clip, double start, double end)
        {
            if (_isPlaying)
            {
                return false;
            }

            if (!Load(clip))
            {
                return false;
            }

            VideoTrimMath.ClampTrim(clip.length, start, end, out _startTime, out _endTime);
            _isPlaying = true;
            _isPaused = false;

            if (_videoPlayer.isPrepared)
            {
                SeekToStart();
            }
            else
            {
                _videoPlayer.Prepare();
            }

            return true;
        }


        public bool Pause()
        {
            if (!_isPlaying || _isPaused)
            {
                return false;
            }

            _isPaused = true;
            _videoPlayer.Pause();

            return true;
        }


        public bool Resume()
        {
            if (!_isPlaying || !_isPaused)
            {
                return false;
            }

            _isPaused = false;
            _videoPlayer.Play();

            return true;
        }


        public bool Stop()
        {
            if (!_isPlaying)
            {
                return false;
            }

            Finish();

            return true;
        }


        /// <summary>
        ///     Ends playback at the end time. Call every editor update.
        /// </summary>
        public void Tick()
        {
            if (!_isPlaying || _isPaused || _isWaitingForSeek || _videoPlayer == null || !_videoPlayer.isPlaying)
            {
                return;
            }

            if (_videoPlayer.time >= _endTime)
            {
                Finish();
            }
        }


        /// <summary>
        ///     Stops playback and destroys the temporary GameObject and the RenderTexture.
        /// </summary>
        public void Dispose()
        {
            Stop();

            if (_videoPlayer != null)
            {
                _videoPlayer.prepareCompleted -= OnPrepareCompleted;
                _videoPlayer.seekCompleted -= OnSeekCompleted;
                _videoPlayer.loopPointReached -= OnLoopPointReached;
                _videoPlayer.targetTexture = null;
            }

            ReleaseRenderTexture();

            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }

            _root = null;
            _videoPlayer = null;
            _audioSource = null;
            _clip = null;
        }


        private void EnsureRig()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject(RootName) {hideFlags = HideFlags.DontSave};

            _audioSource = _root.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            _videoPlayer = _root.AddComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = false;
            _videoPlayer.waitForFirstFrame = true;
            _videoPlayer.skipOnDrop = false; // Keep every frame, so a recording gets all of them
            _videoPlayer.timeUpdateMode = VideoTimeUpdateMode.GameTime; // Follows the Recorder's fixed frame rate
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.controlledAudioTrackCount = 1;
            _videoPlayer.EnableAudioTrack(0, true);
            _videoPlayer.SetTargetAudioSource(0, _audioSource);

            _videoPlayer.prepareCompleted += OnPrepareCompleted;
            _videoPlayer.seekCompleted += OnSeekCompleted;
            _videoPlayer.loopPointReached += OnLoopPointReached;

            if (Object.FindAnyObjectByType<AudioListener>() == null)
            {
                _root.AddComponent<AudioListener>(); // To hear the clip; the Recorder also captures audio from the AudioListener
            }
        }


        private void CreateRenderTexture()
        {
            ReleaseRenderTexture();

            var width = VideoTrimMath.MakeEven((int) _clip.width);
            var height = VideoTrimMath.MakeEven((int) _clip.height);

            _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = $"{_clip.name}_VideoEditor",
                hideFlags = HideFlags.DontSave
            };

            _renderTexture.Create();
            _videoPlayer.targetTexture = _renderTexture;
        }


        private void ReleaseRenderTexture()
        {
            if (_renderTexture == null)
            {
                return;
            }

            if (_videoPlayer != null && _videoPlayer.targetTexture == _renderTexture)
            {
                _videoPlayer.targetTexture = null;
            }

            _renderTexture.Release();
            Object.DestroyImmediate(_renderTexture);
            _renderTexture = null;
        }


        private void OnPrepareCompleted(VideoPlayer source)
        {
            if (_isPlaying)
            {
                SeekToStart();
            }
        }


        private void SeekToStart()
        {
            var halfFrame = _clip.frameRate > 0d ? 0.5d / _clip.frameRate : 0.02d;

            if (Math.Abs(_videoPlayer.time - _startTime) <= halfFrame)
            {
                BeginPlayback();

                return;
            }

            _isWaitingForSeek = true;
            _videoPlayer.time = _startTime;
        }


        private void OnSeekCompleted(VideoPlayer source)
        {
            if (!_isWaitingForSeek)
            {
                return;
            }

            _isWaitingForSeek = false;

            if (_isPlaying)
            {
                BeginPlayback();
            }
        }


        private void BeginPlayback()
        {
            _videoPlayer.Play();
            PlaybackStarted?.Invoke();
        }


        private void OnLoopPointReached(VideoPlayer source)
        {
            if (_isPlaying)
            {
                Finish();
            }
        }


        private void Finish()
        {
            _isPlaying = false;
            _isPaused = false;
            _isWaitingForSeek = false;

            if (_videoPlayer != null && _videoPlayer.isPlaying)
            {
                _videoPlayer.Pause();
            }

            PlaybackFinished?.Invoke();
        }
    }
}
