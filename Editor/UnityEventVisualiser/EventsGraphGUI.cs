using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using Object = UnityEngine.Object;


namespace EventVisualizer.Base
{
    /// <summary>
    /// Draws an EventsGraph in the editor window. Handles node windows, edge rendering and syncing the graph selection to the Editor Selection.
    /// </summary>
    [Serializable]
    public class EventsGraphGUI : GraphGUI
    {
        /// <summary>When non-zero, the id of the node to select and center on during the next GUI pass. Reset to 0 once applied.</summary>
        [SerializeField] public int SelectionOverride;

        /// <summary>The edge renderer for this graph. Created on first access.</summary>
        public override IEdgeGUI edgeGUI
        {
            get
            {
                if (m_EdgeGUI == null)
                {
                    m_EdgeGUI = new EdgeGUI {host = this};
                }

                return m_EdgeGUI;
            }
        }


        /// <summary>Draws the node windows, then the edges, and handles mouse drag selection.</summary>
        public override void OnGraphGUI()
        {
            // Show node subwindows.
            m_Host.BeginWindows();

            foreach (var node in graph.nodes)
            {
                // Recapture the variable for the delegate.
                var node2 = node;

                // Subwindow style (active/nonactive)
                var isActive = selection.Contains(node);
                var style = Styles.GetNodeStyle(node.style, node.color, isActive);

                    node.position = GUILayout.Window(
                    node.GetEntityId().GetHashCode(),
                    node.position,
                    delegate { NodeGUI(node2); },
                    node.title,
                    style,
                    GUILayout.Width(150)
                );
            }

            if (graph.nodes.Count == 0)
            {
                GUILayout.Window(0, new Rect(0, 0, 1, 1), delegate { }, "", "MiniLabel");
            }

            m_Host.EndWindows();

            // Graph edges
            edgeGUI.DoEdges();


            // Mouse drag
            #if UNITY_2017 || UNITY_2017_1_OR_NEWER
            DragSelection();
            #else
			DragSelection(new Rect(-5000, -5000, 10000, 10000));
            #endif
        }


        /// <summary>Draws a node's input slots, body and output slots, then handles dragging and selection for it.</summary>
        /// <param name="node">The node to draw.</param>
        public override void NodeGUI(Node node)
        {
            SelectNode(node);

            foreach (var slot in node.inputSlots)
            {
                LayoutSlot(slot, slot.title, false, true, true, Styles.triggerPinIn);
            }

            node.NodeUI(this);

            foreach (var slot in node.outputSlots)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                LayoutSlot(slot, slot.title, true, false, true, Styles.triggerPinOut);
                EditorGUILayout.EndHorizontal();
            }

            DragNodes();

            UpdateSelection();
        }


        private void UpdateSelection()
        {
            OverrideSelection();

            if (selection.Count == 0)
            {
                _lastPushedSelection = Array.Empty<Object>();

                return;
            }

            var entities = selection.OfType<NodeGUI>().Select(node => node.Entity).Where(entity => entity != null).ToArray();

            // Only push when the graph selection changed, so a selection made elsewhere (Hierarchy) is not
            // overwritten on every GUI event, and Selection is not rewritten constantly.
            if (entities.SequenceEqual(_lastPushedSelection))
            {
                return;
            }

            _lastPushedSelection = entities;
            Selection.objects = entities;
        }


        private Object[] _lastPushedSelection = Array.Empty<Object>();


        private void OverrideSelection()
        {
            if (SelectionOverride != 0)
            {
                var selectedNode = graph[SelectionOverride.ToString()];

                if (selectedNode != null)
                {
                    selection.Clear();
                    selection.Add(selectedNode);
                    CenterGraph(selectedNode.position.position);
                }

                SelectionOverride = 0;
            }
        }
    }
}