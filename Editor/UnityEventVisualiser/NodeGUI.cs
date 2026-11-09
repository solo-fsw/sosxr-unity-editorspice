using System.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;


namespace EventVisualizer.Base
{
    public class NodeGUI : Node
    {
        #region Public class methods

        // Factory method
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

        public bool isValid => _runtimeInstance != null;

        #endregion

        #region Overridden virtual methods

        public override string title => isValid ? _runtimeInstance.Name : "<Missing>";


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