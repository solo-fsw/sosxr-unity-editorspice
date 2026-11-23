using UnityEditor;
using UnityEngine;

/// <summary>
/// Match settings of SceneCamera to MainCamera settings
/// Additional toggle is to also match the transform / rotation
/// Draws icon in Scene view: click to toggle
/// Draws on every update if enabled
/// Off by default: while on, the Scene view's camera settings follow the Main Camera and cannot be changed by hand.
/// </summary>
[InitializeOnLoad]
static class SceneCameraSync
{
    const string Key = "SceneCameraSync.Enabled";
    static bool Enabled
    {
        get => EditorPrefs.GetBool(Key, false);
        set => EditorPrefs.SetBool(Key, value);
    }

    const string FollowKey = "SceneCameraSync.FollowTransform";
    static bool FollowTransform
    {
        get => EditorPrefs.GetBool(FollowKey, false);
        set => EditorPrefs.SetBool(FollowKey, value);
    }

    [MenuItem("SOSXR/Scene Camera Sync/Toggle Follow Transform")]
    static void ToggleFollow() => FollowTransform = !FollowTransform;

    static GUIContent onIcon;
    static GUIContent offIcon;

    static SceneCameraSync()
    {
        EditorApplication.update += Sync;
        SceneView.duringSceneGui += DrawButton;
    }

    [MenuItem("SOSXR/Scene Camera Sync/Toggle")]
    static void Toggle() => Enabled = !Enabled;

    static void DrawButton(SceneView sv)
    {
        onIcon ??= new GUIContent(EditorGUIUtility.IconContent("SceneViewCamera").image, "Scene camera - MainCamera sync: ON (click to disable)");
        offIcon ??= new GUIContent(EditorGUIUtility.IconContent("SceneViewCamera").image, "Scene camera - MainCamera sync: OFF (click to enable)");

        Handles.BeginGUI();
        var rect = new Rect(10, sv.position.height - 60, 32, 32);

        var prev = GUI.backgroundColor;
        GUI.backgroundColor = Enabled ? new Color(0.4f, 1f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);

        if (GUI.Button(rect, Enabled ? onIcon : offIcon))
        {
            Enabled = !Enabled;
            sv.Repaint();
        }

        GUI.backgroundColor = prev;
        Handles.EndGUI();
    }

    static void Sync()
    {
        if (!Enabled || EditorApplication.isCompiling)
        {
            return;
        }

        var cam = Camera.main;
        var sv = SceneView.lastActiveSceneView;
        if (cam == null || sv == null)
        {
            return;
        }

        var cs = sv.cameraSettings;
        cs.dynamicClip = false;
        cs.nearClip = cam.nearClipPlane;
        cs.farClip = cam.farClipPlane;
        cs.fieldOfView = cam.fieldOfView;
        cs.occlusionCulling = cam.useOcclusionCulling;
        sv.cameraSettings = cs;

        sv.orthographic = cam.orthographic;

        if (FollowTransform)
        {
            var rot = cam.transform.rotation;
            var pivot = cam.transform.position + rot * Vector3.forward * sv.cameraDistance;
            sv.LookAt(pivot, rot, sv.size, cam.orthographic, true);
        }

        var st = sv.sceneViewState;
        st.showSkybox = cam.clearFlags == CameraClearFlags.Skybox;
        st.showFog = RenderSettings.fog;
        st.showImageEffects = PostProcessingOn(cam);
        sv.sceneViewState = st;
    }

    static bool PostProcessingOn(Camera cam)
    {
        var urp = cam.GetComponent("UniversalAdditionalCameraData");
        if (urp != null)
        {
            var p = urp.GetType().GetProperty("renderPostProcessing");
            if (p != null)
            {
                return (bool)p.GetValue(urp);
            }
        }
        var ppl = cam.GetComponent("PostProcessLayer") as Behaviour;
        return ppl != null && ppl.enabled;
    }
}
