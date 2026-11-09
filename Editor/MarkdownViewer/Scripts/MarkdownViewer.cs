using System;
using Markdig;
using Markdig.Extensions.JiraLinks;
using Markdig.Extensions.Tables;
using UnityEditor;
using UnityEngine;


namespace MG.MDV
{
    public class MarkdownViewer
    {
        private readonly GUISkin mSkin;
        private readonly string mText = string.Empty;
        private readonly string mCurrentPath = string.Empty;
        private readonly HandlerImages mHandlerImages = new();
        private readonly HandlerNavigate mHandlerNavigate = new();

        private readonly Func<float> mViewWidthProvider = () => EditorGUIUtility.currentViewWidth;

        private readonly Layout mLayout;
        private bool mRaw;


        public MarkdownViewer(GUISkin skin, string path, string content)
        {
            mSkin = skin;
            mCurrentPath = path;
            mText = content;

            mHistory.OnOpen(mCurrentPath);
            mLayout = ParseDocument();

            mHandlerImages.CurrentPath = mCurrentPath;

            mHandlerNavigate.CurrentPath = mCurrentPath;
            mHandlerNavigate.History = mHistory;
            mHandlerNavigate.FindBlock = id => mLayout.Find(id);
            mHandlerNavigate.ScrollTo = pos => { }; // TODO: mScrollPos.y = pos;
        }


        public MarkdownViewer(GUISkin skin, string path, string content, Func<float> viewWidthProvider) : this(skin, path, content)
        {
            if (viewWidthProvider == null)
            {
                throw new ArgumentNullException(nameof(viewWidthProvider));
            }

            mViewWidthProvider = viewWidthProvider;
        }


        public static readonly Vector2 Margin = new(6.0f, 4.0f);

        private static readonly History mHistory = new();


        //------------------------------------------------------------------------------


        public bool Update()
        {
            return mHandlerImages.Update();
        }


        /// <summary>
        ///     Releases pending web requests and the textures downloaded for this document.
        /// </summary>
        public void Dispose()
        {
            mHandlerImages.Dispose();
        }


        //------------------------------------------------------------------------------


        private Layout ParseDocument()
        {
            var context = new Context(mSkin, mHandlerImages, mHandlerNavigate);
            var builder = new LayoutBuilder(context);
            var renderer = new RendererMarkdown(builder);

            var pipelineBuilder = new MarkdownPipelineBuilder()
                    .UseAutoLinks()
                ;

            if (!string.IsNullOrEmpty(Preferences.JIRA))
            {
                pipelineBuilder.UseJiraLinks(new JiraLinkOptions(Preferences.JIRA));
            }


            if (Preferences.PipedTables)
            {
                pipelineBuilder.UsePipeTables(new PipeTableOptions
                {
                    RequireHeaderSeparator = Preferences.PipedTablesRequireRequireHeaderSeparator
                });
            }


            var pipeline = pipelineBuilder.Build();
            pipeline.Setup(renderer);

            var doc = Markdown.Parse(mText, pipeline);
            renderer.Render(doc);

            return builder.GetLayout();
        }


        //------------------------------------------------------------------------------


        private void ClearBackground(float height)
        {
            var rectFullScreen = new Rect(0.0f, 0.0f, Screen.width, Mathf.Max(height, Screen.height));
            GUI.DrawTexture(rectFullScreen, mSkin.window.normal.background, ScaleMode.StretchToFill, false);
        }


        //------------------------------------------------------------------------------


        public void Draw()
        {
            GUI.skin = mSkin;
            GUI.enabled = true;

            // useable width of inspector windows

            var contentWidth = mViewWidthProvider() - mSkin.verticalScrollbar.fixedWidth - 2.0f * Margin.x;


            // draw content

            if (mRaw)
            {
                var style = mSkin.GetStyle("raw");
                var width = contentWidth - mSkin.button.fixedHeight;
                var height = style.CalcHeight(new GUIContent(mText), width);

                ClearBackground(height);
                EditorGUILayout.SelectableLabel(mText, style, GUILayout.Width(width), GUILayout.Height(height));
            }
            else
            {
                ClearBackground(mLayout.Height);
                DrawMarkdown(contentWidth);
            }

            DrawToolbar(contentWidth);
        }


        //------------------------------------------------------------------------------


        private void DrawToolbar(float contentWidth)
        {
            var style = GUI.skin.button;
            var size = style.fixedHeight;
            var btn = new Rect(Margin.x + contentWidth - size, Margin.y, size, size);

            if (GUI.Button(btn, string.Empty, GUI.skin.GetStyle(mRaw ? "btnRaw" : "btnFile")))
            {
                mRaw = !mRaw;
            }

            if (!mRaw)
            {
                if (mHistory.CanForward)
                {
                    btn.x -= size;

                    if (GUI.Button(btn, string.Empty, GUI.skin.GetStyle("btnForward")))
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(mHistory.Forward());
                    }
                }

                if (mHistory.CanBack)
                {
                    btn.x -= size;

                    if (GUI.Button(btn, string.Empty, GUI.skin.GetStyle("btnBack")))
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(mHistory.Back());
                    }
                }
            }
        }


        //------------------------------------------------------------------------------


        private void DrawMarkdown(float width)
        {
            switch (Event.current.type)
            {
                case EventType.Ignore:
                    break;

                case EventType.ContextClick:
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("View Source"), false, () => mRaw = !mRaw);
                    menu.ShowAsContext();

                    break;

                case EventType.Layout:
                    GUILayout.Space(mLayout.Height);
                    mLayout.Arrange(width);

                    break;

                default:
                    mLayout.Draw();

                    break;
            }
        }
    }
}