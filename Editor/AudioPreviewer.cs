using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     Based on Warped Imagination: https://youtu.be/Gd8M1Ychis8?si=8QNBxDnJG4W1X8Cz
    ///     and https://www.youtube.com/watch?v=D8hOWOK8ByY
    /// </summary>
    public static class AudioPreviewer
    {
        private static Assembly AudioImporterAssembly => typeof(AudioImporter).Assembly;
        private static Type AudioUtil => AudioImporterAssembly.GetType("UnityEditor.AudioUtil");
        private static int? _lastPlayedAudioClipId = null;


        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);

            if (obj is not AudioClip audioClip)
            {
                return false; // Unity has control of this asset
            }

            if (IsPreviewClipPlaying())
            {
                StopAllPreviewClips();

                if (_lastPlayedAudioClipId.HasValue && _lastPlayedAudioClipId != instanceID)
                {
                    PlayPreviewClip(audioClip);
                }
            }
            else
            {
                PlayPreviewClip(audioClip);
            }

            _lastPlayedAudioClipId = instanceID;

            return true; // I have control of this asset
        }


        public static void PlayPreviewClip(AudioClip audioClip)
        {
            var methodInfo = AudioUtil.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] {typeof(AudioClip), typeof(int), typeof(bool)},
                null);

            methodInfo?.Invoke(null, new object[] {audioClip, 0, false});
        }


        public static bool IsPreviewClipPlaying()
        {
            var methodInfo = AudioUtil.GetMethod(
                "IsPreviewClipPlaying",
                BindingFlags.Static | BindingFlags.Public);

            return (bool) methodInfo?.Invoke(null, null)!;
        }


        public static void StopAllPreviewClips()
        {
            var methodInfo = AudioUtil.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public);

            methodInfo?.Invoke(null, null);
        }
    }
}