using System.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;


namespace EventVisualizer.Base
{
    /// <summary>
    /// Graph node that displays one NodeData entry from the event visualizer. Its slots are built from the event calls it sends and receives.
    /// </summary>
    public class NodeGUI : Node
    {
        #region Public class methods

        // Factory method
        /// <summary>Creates a NodeGUI bound to the given runtime data. Its icon matches the entity type, GameObject or ScriptableObject.</summary>
        /// <param name="dataInstance">The runtime data to display.</param>
        /// <returns>The new node.</returns>
        public static NodeGUI Create(NodeData dataInstance)
        {
            var isGameObject = dataInstance.Entity is GameObject;

            var node = CreateInstance<NodeGUI>();
            node.Initialize(dataInstance);
            node.name = dataInstance.Entity.GetEntityId().ToString();
            node.icon = (Texture2D) EditorGUIUtility.IconContent(isGameObject ? "Gameobject Icon" : "ScriptableObject Icon").image;

            return node;
        }

        #endregion

        #region Public member properties and methods

        private Texture2D icon;

        /// <summary>True while the node is bound to runtime data. A node without it is shown as Missing.</summary>
        public bool isValid => _runtimeInstance != null;

        /// <summary>The scene object this node represents (null when missing).</summary>
        public Object Entity => _runtimeInstance?.Entity;

        #endregion

        #region Overridden virtual methods

        /// <summary>The name of the node's entity, or Missing when the node is not bound to data.</summary>
        public override string title => isValid ? _runtimeInstance.Name : "<Missing>";


        /// <summary>Draws the base node UI, then the node icon in the top-left corner.</summary>
        /// <param name="host">The graph GUI that hosts the node.</param>
        public override void NodeUI(GraphGUI host)
        {
            base.NodeUI(host);

            if (icon != null)
            {
                GUI.DrawTexture(new Rect(Vector2.one * 5, new Vector2(20, 20)), icon);
            }
        }

        #endregion

        #region Private members

        private NodeData _runtimeInstance;


        private void Initialize(NodeData runtimeInstance)
        {
            hideFlags = HideFlags.DontSave;

            _runtimeInstance = runtimeInstance;
            position = new Rect(Vector2.one * Random.Range(0, 500), Vector2.zero);

            PopulateSlots();
        }


        private void PopulateSlots()
        {
            foreach (var call in _runtimeInstance.Outputs)
            {
                var name = call.eventShortName;
                var title = ObjectNames.NicifyVariableName(name);

                if (!outputSlots.Any(s => s.title == title))
                {
                    var slot = AddOutputSlot(name);
                    slot.title = title;
                }
            }

            foreach (var call in _runtimeInstance.Inputs)
            {
                var name = call.MethodFullPath;
                var title = ObjectNames.NicifyVariableName(name);

                if (!inputSlots.Any(s => s.title == title))
                {
                    var slot = AddInputSlot(name);
                    slot.title = title;
                }
            }
        }


        /// <summary>
        /// Creates a graph edge for each output call to its receiver, replacing any existing edge between the same slots.
        /// Also sets each call's OnTriggered callback to record an edge trigger.
        /// </summary>
        public void PopulateEdges()
        {
            foreach (var outSlot in outputSlots)
            {
                var outCalls = _runtimeInstance.Outputs.FindAll(call => call.eventShortName == outSlot.name);

                foreach (var call in outCalls)
                {
                    var targetNode = graph[call.receiver.GetEntityId().ToString()];
                    var inSlot = targetNode[call.MethodFullPath];

                    if (graph.Connected(outSlot, inSlot))
                    {
                        var existingEdge = graph.edges.Find(e => e.fromSlot == outSlot && e.toSlot == inSlot);
                        graph.RemoveEdge(existingEdge);
                    }

                    var edge = graph.Connect(outSlot, inSlot);
                    // Assign, don't add: PopulateEdges runs on every rebuild
                    call.OnTriggered = () => EdgeTriggersTracker.RegisterTrigger(edge, call);
                }
            }
        }

        #endregion
    }
}