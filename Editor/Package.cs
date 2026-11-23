using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;


namespace SOSXR.EditorSpice.EditorScripts
{
    #if UNITY_EDITOR


    public static class Package
    {
        private static readonly Dictionary<string, bool> PackageCache = new(); // Cache for package status checks.


        static Package()
        {
            // Installing or removing packages makes the cached answers stale.
            Events.registeredPackages += _ => PackageCache.Clear();
        }


        public static void IsInstalled(string packageName, Action<bool> callback)
        {
            if (PackageCache.TryGetValue(packageName, out var cachedResult))
            {
                callback(cachedResult);

                return;
            }

            // Offline, and including indirect dependencies: a package pulled in by another one is installed too.
            var listRequest = Client.List(true, true);


            EditorApplication.update += OnEditorUpdate;

            return;


            void OnEditorUpdate()
            {
                if (!listRequest.IsCompleted)
                {
                    return;
                }

                EditorApplication.update -= OnEditorUpdate; // Remove callback when complete.

                if (listRequest.Status != StatusCode.Success)
                {
                    callback(false); // Treat failure as "not installed."

                    return;
                }

                foreach (var package in listRequest.Result)
                {
                    if (package.name == packageName)
                    {
                        PackageCache[packageName] = true; // Cache positive result.
                        callback(true);

                        return;
                    }
                }

                PackageCache[packageName] = false; // Cache negative result.
                callback(false);
            }
        }
    }


    #endif
}