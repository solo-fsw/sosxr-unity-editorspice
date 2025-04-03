using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using Debug = UnityEngine.Debug;


namespace SOSXR.EditorSpice
{
    [InitializeOnLoad]
    public static class PackageIsInstalled
    {
        static PackageIsInstalled()
        {
            // Subscribe to PackageManager events
            Events.registeredPackages += OnPackagesChanged;
        }


        public static readonly Dictionary<string, bool> Cache = new();


        public static bool PackageInstalled(string packageName)
        {
            if (Cache.TryGetValue(packageName, out var isInstalled))
            {
                Debug.Log("Package " + packageName + " is installed, this was cached");

                return isInstalled;
            }

            Debug.Log("Package " + packageName + " is not installed");

            return Cache.ContainsKey(packageName) && Cache[packageName];
        }


        private static void OnPackagesChanged(PackageRegistrationEventArgs args)
        {
            // Debug.Log("We have a package change, clearing cache");

            // Clear the cache when packages change
            Cache.Clear();

            // Optionally repopulate cache with currently installed packages
            foreach (var package in args.added)
            {
                Cache[package.name] = true;
            }

            foreach (var package in args.removed)
            {
                Cache[package.name] = false;
            }
        }
    }
}