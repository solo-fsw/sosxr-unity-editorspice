using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace SOSXR.EditorSpice
{
    /// <summary>
    ///     Based on Warped Imagination: https://www.youtube.com/watch?v=SY_SdcPW8vE
    /// </summary>
    public class SetIconWindow : EditorWindow
    {
        private List<Texture2D> _iconTextures;
        private List<GUIContent> _cachedIconContents;
        private int _selectedIconIndex;
        private GUIStyle _iconButtonStyle;
        private Vector2 _scrollPos = default;
        private int _iconsPerRow;

        private const string MENU_PATH = "SOSXR/Set Icon... _%i"; // Shortcut: Ctrl/Cmd + I
        private const string ICON_LABEL = "ScriptIcon";
        private const int ICON_WIDTH = 200;
        private const int ICON_HEIGHT = 200;


        [MenuItem(MENU_PATH, priority = 0)]
        public static void ShowMenuItem()
        {
            var window = (SetIconWindow) GetWindow(typeof(SetIconWindow));
            window.titleContent = new GUIContent($"Icons with Label: {ICON_LABEL}");
            window.Show();
        }


        [MenuItem(MENU_PATH, validate = true)]
        public static bool ValidateMenuItem()
        {
            return Selection.objects.All(asset => asset.GetType() == typeof(MonoScript));
        }


        private void OnGUI()
        {
            InitializeIconTextures();

            if (_iconTextures == null || _iconTextures.Count == 0)
            {
                DisplayNoIconsFoundMessage();

                return;
            }

            InitializeGUIStyle();

            EditorGUILayout.LabelField("Select an Icon:", EditorStyles.boldLabel);
            DisplayIconSelectionGrid();

            GUILayout.FlexibleSpace();
            ProcessKeyboardInput();
            DisplayApplyButton();
            DisplayDefaultIconButton();
        }


        private void InitializeIconTextures()
        {
            if (_iconTextures != null)
            {
                return;
            }

            _iconTextures = AssetDatabase.FindAssets($"t:texture2D, l:{ICON_LABEL}")
                                         .Select(AssetDatabase.GUIDToAssetPath)
                                         .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                                         .ToList();

            _cachedIconContents = _iconTextures
                                  .Select(tex => new GUIContent(ScaleTexture(tex, ICON_WIDTH, ICON_HEIGHT)))
                                  .ToList();
        }


        private void DisplayNoIconsFoundMessage()
        {
            GUILayout.Label("No icons found");

            if (GUILayout.Button("Close", GUILayout.ExpandWidth(true)))
            {
                Close();
            }
        }


        private void InitializeGUIStyle()
        {
            _iconButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                fixedWidth = ICON_WIDTH,
                fixedHeight = ICON_HEIGHT,
                alignment = TextAnchor.MiddleCenter
            };
        }


        private void DisplayIconSelectionGrid()
        {
            if (_cachedIconContents == null || _cachedIconContents.Count == 0)
            {
                return;
            }

            var windowWidth = EditorGUIUtility.currentViewWidth;
            _iconsPerRow = Mathf.Max(1, Mathf.FloorToInt(windowWidth / (ICON_WIDTH + 10))); // Dynamic margin

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false);

            _selectedIconIndex = GUILayout.SelectionGrid(
                _selectedIconIndex,
                _cachedIconContents.ToArray(),
                _iconsPerRow,
                _iconButtonStyle
            );

            GUILayout.EndScrollView();
        }


        private void DisplayApplyButton()
        {
            if (GUILayout.Button("Apply New Icon", GUILayout.ExpandWidth(true)))
            {
                ApplySelectedIcon();
            }
        }


        private void DisplayDefaultIconButton()
        {
            if (GUILayout.Button("Reset to Default Icon", GUILayout.ExpandWidth(true)))
            {
                ApplyIcon(null);
                Close();
            }
        }


        private void ApplySelectedIcon()
        {
            ApplyIcon(_iconTextures[_selectedIconIndex]);
            Close();
        }


        private void ApplyIcon(Texture2D icon)
        {
            AssetDatabase.StartAssetEditing();

            foreach (var asset in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(asset);

                if (AssetImporter.GetAtPath(path) is MonoImporter importer)
                {
                    importer.SetIcon(icon);
                    AssetDatabase.ImportAsset(path);
                }
            }

            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }


        private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            var rt = new RenderTexture(targetWidth, targetHeight, 24);
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            var result = new Texture2D(targetWidth, targetHeight);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            rt.Release();
            DestroyImmediate(rt);

            return result;
        }


        private void ProcessKeyboardInput()
        {
            if (Event.current == null || Event.current.type != EventType.KeyDown)
            {
                return;
            }

            switch (Event.current.keyCode)
            {
                case KeyCode.RightArrow: AdjustSelection(1); break;
                case KeyCode.LeftArrow: AdjustSelection(-1); break;
                case KeyCode.UpArrow: AdjustSelection(-_iconsPerRow); break;
                case KeyCode.DownArrow: AdjustSelection(_iconsPerRow); break;
                case KeyCode.Return: ApplySelectedIcon(); break;
                case KeyCode.Escape: Close(); break;
            }
        }


        private void AdjustSelection(int offset)
        {
            var newIndex = _selectedIconIndex + offset;

            if (newIndex < 0 || newIndex >= _iconTextures.Count)
            {
                return;
            }

            _selectedIconIndex = newIndex;
            Event.current.Use();
        }
    }
}