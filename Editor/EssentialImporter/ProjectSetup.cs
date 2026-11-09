using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static System.Environment;
using static System.IO.Path;
using static UnityEditor.AssetDatabase;


namespace SOSXR.Setup
{
    /// <summary>
    ///     Based on git-amend: https://www.youtube.com/watch?v=0_ZRHT2faQw&t=77s
    /// </summary>
    public static class ProjectSetup
    {
        [MenuItem("SOSXR/Setup/Import Essential Assets")]
        public static void ImportEssentials()
        {
            Assets.ImportAsset("DOTween HOTween v2.unitypackage", "FlyingWorm/Editor ExtensionsSystem");

            if (!Assets.ImportAsset("Mantis LOD Editor - Professional Edition.unitypackage", "Mesh Online/Editor ExtensionsModeling"))
            {
                Assets.ImportAsset("Ultimate LOD System MT - Automatic LOD Generator Mesh Simplifier More.unitypackage", "MT Assets/Editor ExtensionsGame Toolkits");
            }

            Assets.ImportAsset("Gridbox Prototype Materials.unitypackage", "Ciathyza/Textures Materials");
            Assets.ImportAsset("Play Mode Saver.unitypackage", "Clarky/Editor ExtensionsSystem");
            Assets.ImportAsset("3D Measurements.unitypackage", "Energise/Editor ExtensionsSystem");
            Assets.ImportAsset("F Texture Tools.unitypackage", "FImpossible Creations/Editor ExtensionsSystem");
            Assets.ImportAsset("Editor Console Pro.unitypackage", "HeurekaGames/Editor ExtensionsUtilities");
            Assets.ImportAsset("Asset Hunter PRO.unitypackage", "Demigiant/Editor ExtensionsAnimation");
            Assets.ImportAsset("Mesh Baker.unitypackage", "Ian Deane/ScriptingModeling");
            Assets.ImportAsset("Colourful Hierarchy Category GameObject.unitypackage", "M STUDIO HUB/Editor ExtensionsUtilities");
            Assets.ImportAsset("Smart Editor Selection.unitypackage", "Overfort Games/Editor ExtensionsDesign");
            Assets.ImportAsset("UMotion Pro - Animation Editor.unitypackage", "Soxware Interactive/Editor ExtensionsAnimation");
            Assets.ImportAsset("Better Transform - Size Notes Global-Local workspace child parent transform.unitypackage", "Tiny Giant Studio/Editor ExtensionsUtilities");

            #if !UNITY_EDITOR_OSX
            Assets.ImportAsset("Bakery - GPU Lightmapper.unitypackage", "Mr F/Editor ExtensionsDesign");
            #endif
        }


        /// <summary>
        ///     Gets the latest stable packages from the SOSXR repositories, and other essential packages.
        /// </summary>
        [MenuItem("SOSXR/Setup/Install Essential Packages - Stable")]
        public static void InstallStablePackages()
        {
            Packages.InstallPackages(new[]
            {
                "com.unity.ide.rider",
                "com.unity.mobile.android-logcat",
                "com.unity.nuget.newtonsoft-json",
                "com.unity.cloud.gltfast",
                "com.unity.modules.imageconversion",
                "com.unity.memoryprofiler",
                "git+https://github.com/KyleBanks/scene-ref-attribute.git",
                "git+https://github.com/solo-fsw/sosxr-unity-enhancedlogger.git",
                "git+https://github.com/solo-fsw/sosxr-unity-plet.git",
                "git+https://github.com/solo-fsw/sosxr-unity-editorspice.git",
                "git+https://github.com/solo-fsw/sosxr-unity-scriptableobjectarchitecture.git",
                "git+https://github.com/solo-fsw/sosxr-unity-timelineextensions.git",
                "git+https://github.com/arimger/Unity-Editor-Toolbox.git#upm",
                "git+https://github.com/XCharts-Team/XCharts.git",
                "com.unity.inputsystem" // If necessary, import new Input System last as it requires a Unity Editor restart
            });
        }


        /// <summary>
        ///     Pulls from dev branches of SOSXR packages, and other essential packages.
        /// </summary>
        [MenuItem("SOSXR/Setup/Install Essential Packages - Beta")]
        public static void InstallBetaPackages()
        {
            Packages.InstallPackages(new[]
            {
                "com.unity.ide.rider",
                "com.unity.mobile.android-logcat",
                "com.unity.nuget.newtonsoft-json",
                "com.unity.cloud.gltfast",
                "com.unity.modules.imageconversion",
                "com.unity.memoryprofiler",
                "git+https://github.com/KyleBanks/scene-ref-attribute.git",
                "git+https://github.com/solo-fsw/sosxr-unity-enhancedlogger.git#dev",
                "git+https://github.com/solo-fsw/sosxr-unity-plet.git#dev",
                "git+https://github.com/solo-fsw/sosxr-unity-editorspice.git#dev",
                "git+https://github.com/solo-fsw/sosxr-unity-scriptableobjectarchitecture.git#dev",
                "git+https://github.com/solo-fsw/sosxr-unity-timelineextensions.git#dev",
                "git+https://github.com/arimger/Unity-Editor-Toolbox.git#upm",
                "git+https://github.com/XCharts-Team/XCharts.git",
                "com.unity.inputsystem" // If necessary, import new Input System last as it requires a Unity Editor restart
            });
        }


        [MenuItem("SOSXR/Setup/Create Folders", priority = 1)]
        public static void CreateFolders()
        {
            Folders.Create("_SOSXR", "Textures & Materials", "Models", "Animation", "Prefabs", "Swatches", "Rendering", "XR", "Input", "Collected Data", "Resources");

            Refresh();
            Folders.Move("_SOSXR", "Scenes");
            Folders.Move("_SOSXR", "Settings");
            Folders.Move("_SOSXR", "Scripts");
            Folders.Delete("TutorialInfo");
            Refresh();

            Folders.Create("_SOSXR", "Scenes", "Settings", "Scripts");
            Refresh();

            Folders.Rename("_SOSXR/Scenes", "_SOSXR/_Scenes");
            Folders.Rename("_SOSXR/Scripts", "_SOSXR/_Scripts");
            Refresh();

            MoveAsset("Assets/InputSystem_Actions.inputactions", "Assets/_SOSXR/Settings/InputSystem_Actions.inputactions");
            DeleteAsset("Assets/Readme.asset");
            Refresh();

            // Optional: Disable Domain Reload
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        }


        private static class Assets
        {
            public static bool ImportAsset(string asset, string folder)
            {
                string basePath;

                if (OSVersion.Platform is PlatformID.MacOSX or PlatformID.Unix)
                {
                    var homeDirectory = GetFolderPath(SpecialFolder.Personal);
                    basePath = Combine(homeDirectory, "Library/Unity/Asset Store-5.x");
                }
                else
                {
                    var defaultPath = Combine(GetFolderPath(SpecialFolder.ApplicationData), "Unity");
                    basePath = Combine(EditorPrefs.GetString("AssetStoreCacheRootPath", defaultPath), "Asset Store-5.x");
                }

                asset = asset.EndsWith(".unitypackage") ? asset : asset + ".unitypackage";

                var fullPath = Combine(basePath, folder, asset);

                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"The asset package was not found at the path: {fullPath}");

                    return false;
                }

                UnityEditor.AssetPackage.Package.Import(fullPath, false);

                return true;
            }
        }


        private static class Packages
        {
            private static AddAndRemoveRequest request;


            /// <summary>
            ///     Installs all packages in one Package Manager request. One resolve means one domain reload,
            ///     so the install no longer stops after the first package or two.
            /// </summary>
            public static void InstallPackages(string[] packages)
            {
                if (packages == null || packages.Length == 0)
                {
                    return;
                }

                if (request != null && !request.IsCompleted)
                {
                    Debug.LogWarning("A package install is already running. Wait for it to finish.");

                    return;
                }

                request = Client.AddAndRemove(packages);
                EditorApplication.update -= Progress;
                EditorApplication.update += Progress;
            }


            private static void Progress()
            {
                if (request == null)
                {
                    EditorApplication.update -= Progress;

                    return;
                }

                if (!request.IsCompleted)
                {
                    return;
                }

                EditorApplication.update -= Progress;

                if (request.Status == StatusCode.Success)
                {
                    Debug.Log("Installed essential packages.");
                }
                else if (request.Status >= StatusCode.Failure)
                {
                    Debug.LogError(request.Error?.message);
                }

                request = null;
            }
        }


        private static class Folders
        {
            public static void Create(string root, params string[] folders)
            {
                var fullpath = Combine(Application.dataPath, root);

                if (!Directory.Exists(fullpath))
                {
                    Directory.CreateDirectory(fullpath);
                }

                foreach (var folder in folders)
                {
                    CreateSubFolders(fullpath, folder);
                }
            }


            private static void CreateSubFolders(string rootPath, string folderHierarchy)
            {
                var folders = folderHierarchy.Split('/');
                var currentPath = rootPath;

                foreach (var folder in folders)
                {
                    currentPath = Combine(currentPath, folder);

                    if (Directory.Exists(currentPath))
                    {
                        continue;
                    }

                    Directory.CreateDirectory(currentPath);
                }
            }


            public static void Move(string newParent, string folderName)
            {
                var sourcePath = $"Assets/{folderName}";

                if (!IsValidFolder(sourcePath))
                {
                    return;
                }

                var destinationPath = $"Assets/{newParent}/{folderName}";

                if (Directory.Exists(destinationPath))
                {
                    return;
                }

                var error = MoveAsset(sourcePath, destinationPath);

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"Failed to move {folderName}: {error}");
                }
            }


            public static void Delete(string folderName)
            {
                var pathToDelete = $"Assets/{folderName}";

                if (!IsValidFolder(pathToDelete))
                {
                    return;
                }

                DeleteAsset(pathToDelete);
            }


            public static void Rename(string oldName, string newName)
            {
                var oldPath = $"Assets/{oldName}";
                var newPath = $"Assets/{newName}";

                if (!IsValidFolder(oldPath))
                {
                    Debug.LogError($"Folder '{oldName}' does not exist.");

                    return;
                }

                if (IsValidFolder(newPath))
                {
                    Debug.LogError($"A folder named '{newName}' already exists.");

                    return;
                }

                var error = MoveAsset(oldPath, newPath);

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"Failed to rename folder '{oldName}' to '{newName}': {error}");
                }
                else
                {
                    Debug.Log($"Successfully renamed folder '{oldName}' to '{newName}'.");
                }
            }
        }
    }
}