using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;


namespace SOSXR.EditorSpice.EditorScripts
{
    /// <summary>
    ///     Synchronous check whether a package is registered in the project (direct or indirect dependency).
    ///     Results are cached until the Package Manager reports a change.
    /// </summary>
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
            if (string.IsNullOrEmpty(packageName))
            {
                return false;
            }

            if (Cache.TryGetValue(packageName, out var isInstalled))
            {
                return isInstalled;
            }

            // Ask the Package Manager instead of relying on change events only: packages installed before this class
            // was first used never raise an event, so they were reported as not installed.
            isInstalled = PackageInfo.GetAllRegisteredPackages().Any(package => package.name == packageName);
            Cache[packageName] = isInstalled;

            return isInstalled;
        }


        private static void OnPackagesChanged(PackageRegistrationEventArgs args)
        {
            // Debug.Log("We have a package change, clearing cache");

            // Packages changed: the next PackageInstalled call asks the Package Manager again.
            Cache.Clear();
        }
    }
}