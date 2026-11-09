using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Tarodev
{
    /// <summary>
    /// Editor utility that wires up the AutoSaveConfig and drives interval-based saving
    /// of open scenes in the Unity Editor.
    /// </summary>
    // Original descriptive header preserved in the class summary above
    [CustomEditor(typeof(AutoSaveConfig))]
    public class TarodevAutoSave : Editor
    {
        private static AutoSaveConfig _config;
        private static CancellationTokenSource _tokenSource;
        private static Task _task;


        private const string DefaultConfigPath = "Assets/" + nameof(AutoSaveConfig) + ".asset";


        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            // Defer: during InitializeOnLoad the AssetDatabase may not have imported the config yet.
            // Creating it then could overwrite an existing Assets/AutoSaveConfig.asset on a fresh import.
            EditorApplication.delayCall -= Start;
            EditorApplication.delayCall += Start;
        }


        private static void Start()
        {
            FetchConfig();
            CancelTask();

            if (_config == null)
            {
                return;
            }

            _tokenSource = new CancellationTokenSource();
            _task = SaveInterval(_tokenSource.Token);
        }


        private static void FetchConfig()
        {
            if (_config != null)
            {
                return;
            }

            var path = GetConfigPath();

            if (path == null)
            {
                // Never overwrite an asset that already sits at the default path
                if (AssetDatabase.LoadMainAssetAtPath(DefaultConfigPath) != null || File.Exists(DefaultConfigPath))
                {
                    _config = AssetDatabase.LoadAssetAtPath<AutoSaveConfig>(DefaultConfigPath);

                    return;
                }

                AssetDatabase.CreateAsset(CreateInstance<AutoSaveConfig>(), DefaultConfigPath);
                Debug.Log("A config file has been created at the root of your project.<b> You can move this anywhere you'd like.</b>");
                path = DefaultConfigPath;
            }

            _config = AssetDatabase.LoadAssetAtPath<AutoSaveConfig>(path);
        }


        private static string GetConfigPath()
        {
            // Search by type, not by name: a name search also matches unrelated assets and scripts
            var paths = AssetDatabase.FindAssets($"t:{nameof(AutoSaveConfig)}").Select(AssetDatabase.GUIDToAssetPath).Where(c => c.EndsWith(".asset")).ToList();

            if (paths.Count > 1)
            {
                Debug.LogWarning("Multiple auto save config assets found. Delete until you have only one.");
            }

            return paths.FirstOrDefault();
        }


        private static void CancelTask()
        {
            if (_task == null)
            {
                return;
            }

            // Don't Wait() here: the task resumes on the main thread, so waiting on it can deadlock the editor
            _tokenSource.Cancel();
            _tokenSource.Dispose();
            _tokenSource = null;
            _task = null;
        }


        /// <summary>
        ///     True when the editor is in a state where auto-saving is safe.
        /// </summary>
        public static bool CanAutoSave(bool enabled, bool isPlayingOrWillChangePlaymode, bool isBuildingPlayer, bool isCompiling, bool isApplicationActive)
        {
            return enabled && !isPlayingOrWillChangePlaymode && !isBuildingPlayer && !isCompiling && isApplicationActive;
        }


        /// <summary>
        ///     True when a scene can be saved without a Save-As dialog: it has a path on disk and has changes.
        /// </summary>
        public static bool CanSaveSceneSilently(string scenePath, bool isDirty)
        {
            return isDirty && !string.IsNullOrEmpty(scenePath);
        }


        private static async Task SaveInterval(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(Mathf.Max(1, _config != null ? _config.Frequency : 1) * 1000 * 60, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (_config == null)
                {
                    FetchConfig();
                }

                if (_config == null)
                {
                    continue;
                }

                if (!CanAutoSave(_config.Enabled, EditorApplication.isPlayingOrWillChangePlaymode, BuildPipeline.isBuildingPlayer, EditorApplication.isCompiling, InternalEditorUtility.isApplicationActive))
                {
                    continue;
                }

                // SaveOpenScenes() opens a Save-As dialog for untitled scenes on every interval. Only save scenes that have a path.
                var saved = 0;

                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);

                    if (scene.IsValid() && scene.isLoaded && CanSaveSceneSilently(scene.path, scene.isDirty) && EditorSceneManager.SaveScene(scene))
                    {
                        saved++;
                    }
                }

                if (_config.Logging && saved > 0)
                {
                    Debug.Log($"Auto-saved at {DateTime.Now:h:mm:ss tt}");
                }
            }
        }


        [MenuItem("SOSXR/AutoSave/Find config")]
        public static void ShowConfig()
        {
            FetchConfig();

            if (_config != null)
            {
                EditorGUIUtility.PingObject(_config);
            }
        }


        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("You can move this asset where ever you'd like.\nWith ❤, Tarodev.", MessageType.Info);
        }
    }
}
