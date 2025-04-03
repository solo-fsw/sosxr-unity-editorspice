using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice.Helpers
{
    public static class UnityEditorBackgroundColor
    {
        private static readonly Color k_defaultcolor = new(0.7843f, 0.7843f, 0.7843f);
        private static readonly Color k_defaultProColor = new(0.2196f, 0.2196f, 0.2196f);
        private static readonly Color k_selectedColor = new(0.22745f, 0.447f, 0.6902f);
        private static readonly Color k_selectedProColor = new(0.1725f, 0.3647f, 0.5294f);
        private static readonly Color k_selectedUnFocusedColor = new(0.68f, 0.68f, 0.68f);
        private static readonly Color k_selectedUnFocusedProColor = new(0.3f, 0.3f, 0.3f);
        private static readonly Color k_hoveredcolor = new(0.698f, 0.698f, 0.698f);
        private static readonly Color k_hoveredProColor = new(0.2706f, 0.2706f, 0.2706f);


        public static Color Get(bool isSelected, bool isHovererd, bool isWindowFocused)
        {
            if (isSelected)
            {
                if (!isWindowFocused)
                {
                    return EditorGUIUtility.isProSkin ? k_hoveredProColor : k_hoveredcolor;
                }

                return EditorGUIUtility.isProSkin ? k_selectedUnFocusedProColor : k_selectedUnFocusedColor;
            }

            if (!isHovererd)
            {
                return EditorGUIUtility.isProSkin ? k_hoveredProColor : k_hoveredcolor;
            }

            return EditorGUIUtility.isProSkin ? k_defaultProColor : k_defaultcolor;
        }
    }
}