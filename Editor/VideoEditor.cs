using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.Video;


public class VideoEditor : MonoBehaviour
{
    public Vector2 Trim;

    private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseMap");
    private RenderTexture renderTexture;
    private Vector2 aspectRatio = new(1, 1);
    private VideoPlayer videoPlayer;
    private Camera previewCamera;
    private GameObject quad;
    private Material previewMaterial;
    private RecorderControllerSettings recorderControllerSettings;
    private MovieRecorderSettings movieRecorderSettings;
    private RenderTextureInputSettings imageInputSettings;
    private RecorderController recorderController;
    private VideoClip _storedClip;
    private Vector2 _storedTrim;


    [ContextMenu(nameof(Awake))]
    private void Awake()
    {
        recorderControllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        recorderController = new RecorderController(recorderControllerSettings);

        movieRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();

        movieRecorderSettings.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
        };

        movieRecorderSettings.RecordMode = RecordMode.Manual;

        recorderControllerSettings.AddRecorderSettings(movieRecorderSettings);

        var videoPlayerObject = new GameObject("VideoPlayer");
        videoPlayerObject.transform.SetParent(gameObject.transform);
        videoPlayer = videoPlayerObject.AddComponent<VideoPlayer>();
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.aspectRatio = VideoAspectRatio.FitVertically;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        var cameraObj = new GameObject("VideoPreviewCamera");
        cameraObj.transform.SetParent(gameObject.transform);
        previewCamera = cameraObj.AddComponent<Camera>();
        previewCamera.transform.position = Vector3.zero;
        previewCamera.depth = 100;

        previewMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); // Change to Unlit
        previewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        previewMaterial.SetFloat("_Surface", 0);
        previewMaterial.SetFloat("_Blend", 0);

        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(gameObject.transform);
        quad.transform.position = new Vector3(0, 0, 1);
        var rend = quad.GetComponent<MeshRenderer>();
        rend.material = previewMaterial;
    }


    private void VideoClipChanged()
    {
        if (videoPlayer.clip == null || videoPlayer.clip == _storedClip)
        {
            return;
        }

        var videoPath = AssetDatabase.GetAssetPath(videoPlayer.clip);
        var videoName = Path.GetFileNameWithoutExtension(videoPath);
        var videoDirectory = Path.GetDirectoryName(videoPath);
        var videoOutputPath = $"{videoDirectory}/{videoName}_Trimmed";
        var videoExtension = ".mp4";
        var pathWithExtension = videoOutputPath + videoExtension;

        if (File.Exists(pathWithExtension))
        {
            videoOutputPath = $"{videoDirectory}/{videoName}_Trimmed_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            Debug.Log("File already existed at simple path. Adding date+time. New path: " + videoOutputPath);
        }

        movieRecorderSettings.OutputFile = videoOutputPath;

        aspectRatio.x = (float) Math.Round((float) videoPlayer.clip.width / videoPlayer.clip.height, 3);
        aspectRatio.y = 1;
        quad.transform.localScale = new Vector3(aspectRatio.x, aspectRatio.y, 1);

        recorderControllerSettings.FrameRate = (float) videoPlayer.clip.frameRate;

        ResetRenderTexture();

        Trim = new Vector2(0, (float) videoPlayer.clip.length);

        _storedClip = videoPlayer.clip;
        videoPlayer.Prepare();
    }


    private void ResetTrimXSettings()
    {
        if (Mathf.Approximately(Trim.x, _storedTrim.x))
        {
            return;
        }

        movieRecorderSettings.FrameRate = (float) videoPlayer.clip.frameRate;
        movieRecorderSettings.StartFrame = (int) (Trim.x * movieRecorderSettings.FrameRate);
        // movieRecorderSettings.EndFrame = (int) (Trim.y * movieRecorderSettings.FrameRate);

        videoPlayer.time = Trim.x;
        videoPlayer.Play();
        videoPlayer.Pause();

        _storedTrim = Trim;
        Debug.Log("Trim set to 0-" + Trim.y + " seconds. StartFrame: " + movieRecorderSettings.StartFrame + " EndFrame: " + movieRecorderSettings.EndFrame);
    }


    private void ResetTrimYSettings()
    {
        if (Mathf.Approximately(Trim.y, _storedTrim.y))
        {
            return;
        }

        movieRecorderSettings.FrameRate = (float) videoPlayer.clip.frameRate;
        // movieRecorderSettings.StartFrame = (int) (Trim.x * movieRecorderSettings.FrameRate);
        movieRecorderSettings.EndFrame = (int) (Trim.y * movieRecorderSettings.FrameRate);

        videoPlayer.time = Trim.y;
        videoPlayer.Play();
        videoPlayer.Pause();

        _storedTrim = Trim;
        Debug.Log("Trim set to 0-" + Trim.y + " seconds. StartFrame: " + movieRecorderSettings.StartFrame + " EndFrame: " + movieRecorderSettings.EndFrame);
    }


    private void ResetRenderTexture()
    {
        DestroyExistingTexture();

        renderTexture = new RenderTexture((int) videoPlayer.clip.width, (int) videoPlayer.clip.height, 24, RenderTextureFormat.ARGB32);
        renderTexture.name = "VideoRenderTexture";

        renderTexture.Create();

        previewMaterial.SetTexture(BaseMapProperty, renderTexture);

        videoPlayer.targetTexture = renderTexture;

        imageInputSettings = new RenderTextureInputSettings
        {
            OutputWidth = (int) videoPlayer.clip.width,
            OutputHeight = (int) videoPlayer.clip.height,
            RenderTexture = renderTexture
        };

        movieRecorderSettings.ImageInputSettings = imageInputSettings;
    }


    private void Update()
    {
        VideoClipChanged();
        ResetTrimXSettings();
        ResetTrimYSettings();
    }


    private void StopRecording()
    {
        if (!recorderController.IsRecording())
        {
            Debug.LogWarning("Recorder is not currently recording.");

            return;
        }

        recorderController.StopRecording();
        Debug.Log($"Recording stopped. Video saved to: {movieRecorderSettings.OutputFile}.mp4");
    }


    private void GoToStart()
    {
        videoPlayer.time = 0;
        videoPlayer.Play();
        videoPlayer.Pause();
    }


    [ContextMenu(nameof(Record))]
    public bool Record()
    {
        if (videoPlayer.clip == null)
        {
            Debug.LogWarning("No clip assigned to the VideoPlayer.");

            return false;
        }

        recorderController.PrepareRecording();
        recorderController.StartRecording();
        Debug.Log("Recording started.");

        if (!videoPlayer.isPrepared)
        {
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += _ => { videoPlayer.Play(); };
            Debug.LogWarning("We were not prepared.");
        }
        else
        {
            videoPlayer.Play();
            Debug.Log("We were prepared to start playing.");
        }

        return true;
    }


    public void Testes()
    {
        Debug.Log("Testes");
    }


    [ContextMenu(nameof(Play))]
    public bool Play()
    {
        if (videoPlayer.clip == null)
        {
            Debug.LogWarning("No clip assigned to the VideoPlayer.");

            return false;
        }

        if (!videoPlayer.isPrepared)
        {
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += _ => { videoPlayer.Play(); };
            Debug.LogWarning("We were not prepared.");
        }
        else
        {
            videoPlayer.Play();
            Debug.Log("We were prepared.");
        }

        return true;
    }


    [ContextMenu(nameof(Pause))]
    public bool Pause()
    {
        if (!videoPlayer.isPlaying)
        {
            Debug.LogWarning("VideoPlayer is not playing.");

            return false;
        }

        videoPlayer.Pause();

        return true;
    }


    [ContextMenu(nameof(Stop))]
    public bool Stop()
    {
        if (!videoPlayer.isPlaying)
        {
            Debug.LogWarning("VideoPlayer is not playing.");

            return false;
        }

        videoPlayer.Stop();

        if (recorderController.IsRecording())
        {
            Debug.Log("Recording stopped.");

            StopRecording();
        }

        GoToStart();

        return true;
    }


    private void OnDisable()
    {
        DestroyExistingTexture();
    }


    private void DestroyExistingTexture()
    {
        if (renderTexture == null)
        {
            return;
        }

        renderTexture.Release();
        DestroyImmediate(renderTexture);
    }
}