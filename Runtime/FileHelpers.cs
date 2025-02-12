using System;
using System.IO;
using System.Linq;
using UnityEngine;


public static class FileHelpers
{
    /// <summary>
    ///     Returns the home directory path based on the platform.
    ///     - Android (Runtime, not Editor): Application's external files directory (e.g.,
    ///     "/storage/emulated/0/Android/data/{packageName}/files")
    ///     - macOS (Editor & Standalone): "/Users/{username}"
    ///     - Windows (Editor & Standalone): "C:\Users\{username}"
    ///     - Other platforms: Unsupported message.
    /// </summary>
    public static string GetHomePath()
    {
        var path = "";

        #if UNITY_ANDROID && !UNITY_EDITOR
        using var unityPlayer = new UnityEngine.AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unityPlayer.GetStatic<UnityEngine.AndroidJavaObject>("currentActivity");
        path = activity.Call<UnityEngine.AndroidJavaObject>("getExternalFilesDir", null).Call<string>("getAbsolutePath");
        #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        #elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        #else
        path = "Home directory path is not supported on this platform.";
        #endif

        return path;
    }


    /// <summary>
    ///     Returns the Movies directory path based on the platform.
    ///     - Android: "/storage/emulated/0/Movies"
    ///     - macOS: "/Users/{username}/Movies"
    ///     - Windows: "C:\\Users\\{username}\\Videos"
    ///     - Other platforms: Unsupported message
    /// </summary>
    public static string GetMoviesPath()
    {
        var path = "";

        #if UNITY_ANDROID && !UNITY_EDITOR
        using var envClass = new UnityEngine.AndroidJavaClass("android.os.Environment");
        using var moviesDir = envClass.CallStatic<UnityEngine.AndroidJavaObject>("getExternalStoragePublicDirectory", "Movies");
        path = moviesDir.Call<string>("getAbsolutePath");
        #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Movies");
        #elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyVideos));
        #else
        path = "Movies directory path is not supported on this platform.";
        #endif

        return path;
    }


    /// <summary>
    ///     Returns the ArborXR videos directory path based on the platform.
    ///     - Android: Attempting to access "/storage/emulated/0/ArborXR/videos"
    ///     - macOS: "/Users/{username}/ArborXR/videos"
    ///     - Other platforms: Unsupported message
    /// </summary>
    public static string GetArborXRPath()
    {
        var path = "";
        var arborVideoFolder = "ArborXR/videos";

        #if UNITY_ANDROID && !UNITY_EDITOR
        using var envClass = new UnityEngine.AndroidJavaClass("android.os.Environment");
        using var moviesDir = envClass.CallStatic<UnityEngine.AndroidJavaObject>("getExternalStoragePublicDirectory", arborVideoFolder);
        path = moviesDir.Call<string>("getAbsolutePath");
        #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), arborVideoFolder);
        #else
        path = "ArborXR directory path is not supported on this platform.";
        #endif

        return path;
    }


    /// <summary>
    ///     Returns the Documents directory path based on the platform.
    ///     - Android: "/storage/emulated/0/Documents"
    ///     - macOS: "/Users/{username}/Documents"
    ///     - Windows: "C:\\Users\\{username}\\Documents"
    ///     - Other platforms: Unsupported message
    /// </summary>
    public static string GetDocumentsPath()
    {
        var path = "";

        #if UNITY_ANDROID && !UNITY_EDITOR
        using var envClass = new UnityEngine.AndroidJavaClass("android.os.Environment");
        using var moviesDir = envClass.CallStatic<UnityEngine.AndroidJavaObject>("getExternalStoragePublicDirectory", "Documents");
        path = moviesDir.Call<string>("getAbsolutePath");
        #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents");
        #elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments));
        #else
        path = "Documents directory path is not supported on this platform.";
        #endif

        return path;
    }


    public static string[] GetFileNamesFromDirectory(string[] extensions = null, bool excludeExtensions = false, bool excludeHidden = false, string basePath = null, string subFolder = null)
    {
        var path = basePath + (string.IsNullOrEmpty(subFolder) ? "" : Path.DirectorySeparatorChar + subFolder);

        if (!Directory.Exists(path))
        {
            Debug.LogErrorFormat("Directory does not exist: " + path);

            return Array.Empty<string>(); // Return an empty array if the directory does not exist
        }

        var filenames = Directory.GetFiles(path);

        // Filter by extensions
        if (extensions is {Length: > 0})
        {
            filenames = filenames.Where(f =>
                excludeExtensions
                    ? !extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase)
                    : extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase)
            ).ToArray();
        }

        // Filter out hidden files
        if (excludeHidden)
        {
            filenames = filenames.Where(f => !Path.GetFileName(f).StartsWith(".")).ToArray();
        }

        // Optionally, just get the filenames without their paths
        for (var i = 0; i < filenames.Length; i++)
        {
            filenames[i] = Path.GetFileName(filenames[i]);
        }

        return filenames;
    }
}