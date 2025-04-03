using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


// By Harry Rose : https://github.com/Unity-Harry/Unity-AssetDependencyGraph


namespace SOSXR.AssetDependencyGraph
{
    public class AssetGraphView : GraphView
    {
        public AssetGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            var background = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(0.17f, 0.17f, 0.17f, 1f)
                }
            };

            Insert(0, background);

            background.StretchToParentSize();
        }
    }
}