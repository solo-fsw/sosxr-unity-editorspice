using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     From Warped Imagination: https://www.youtube.com/watch?v=_9wLXAGUgKs
    /// </summary>
    public static class LayoutSwitcherTool
    {
        [Shortcut("SOSXR/Layout Switcher: Open Layout Default", KeyCode.Alpha0, ShortcutModifiers.Alt)]
        public static void OpenLayoutDefault()
        {
            OpenLayout("Default");
        }


        [Shortcut("SOSXR/Layout Switcher: Open Layout 1", KeyCode.Alpha1, ShortcutModifiers.Alt)]
        public static void OpenLayout1()
        {
            OpenLayout("MBP-1");
        }


        [Shortcut("SOSXR/Layout Switcher: Open Layout 2", KeyCode.Alpha2, ShortcutModifiers.Alt)]
        public static void OpenLayout2()
        {
            OpenLayout("MBP-2");
        }


        [Shortcut("SOSXR/Layout Switcher: Open Layout 3E", KeyCode.Alpha3, ShortcutModifiers.Alt)]
        public static void OpenLayout3E()
        {
            OpenLayout("MBP-3E");
        }


        private static bool OpenLayout(string name)
        {
            var path = GetWindowLayoutPath(name);

            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var windowLayoutType = typeof(Editor).Assembly.GetType("UnityEditor.WindowLayout");

            if (windowLayoutType != null)
            {
                var tryLoadWindowLayoutMethod = windowLayoutType.GetMethod("LoadWindowLayout", BindingFlags.Public | BindingFlags.Static, null, new[] {typeof(string), typeof(bool)}, null);

                if (tryLoadWindowLayoutMethod != null)
                {
                    object[] arguments = {path, false};
                    var result = (bool) tryLoadWindowLayoutMethod.Invoke(null, arguments);

                    return result;
                }
            }

            return false;
        }


        private static string GetWindowLayoutPath(string name)
        {
            var layoutsPreferencesPath = Path.Combine(InternalEditorUtility.unityPreferencesFolder, "Layouts");
            var layoutsModePreferencesPath = Path.Combine(layoutsPreferencesPath, ModeService.currentId);

            if (Directory.Exists(layoutsModePreferencesPath))
            {
                var layoutPaths = Directory.GetFiles(layoutsModePreferencesPath).Where(path => path.EndsWith(".wlt")).ToArray();

                foreach (var layoutPath in layoutPaths)
                {
                    if (string.CompareOrdinal(name, Path.GetFileNameWithoutExtension(layoutPath)) == 0)
                    {
                        return layoutPath;
                    }
                }
            }

            return null;
        }
    }
}