using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;


namespace EventVisualizer.Base
{
    [Serializable]
    public class NodeData
    {
        private static Dictionary<int, NodeData> nodes = new();


        public NodeData(Object entity)
        {
            Entity = entity;
            Outputs = new List<EventCall>();
            Inputs = new List<EventCall>();
        }


        public Object Entity { get; private set; }

        public string Name => Entity != null ? Entity.name : "<Missing>";

        public List<EventCall> Outputs { get; private set; }
        public List<EventCall> Inputs { get; private set; }

        public static ICollection<NodeData> Nodes => nodes != null ? nodes.Values : null;


        public static void ClearAll()
        {
            DetachAll();
            nodes.Clear();
        }


        public static void ClearSlots()
        {
            DetachAll();

            foreach (var node in nodes.Keys)
            {
                nodes[node].Outputs.Clear();
                nodes[node].Inputs.Clear();
            }
        }


        public static void RegisterEvent(EventCall eventCall)
        {
            var nodeSender = CreateNode(eventCall.sender);
            var nodeReceiver = CreateNode(eventCall.receiver);

            eventCall.nodeSender = nodeSender;
            eventCall.nodeReceiver = nodeReceiver;

            nodeSender.Outputs.Add(eventCall);
            nodeReceiver.Inputs.Add(eventCall);

            eventCall.Attach();
        }


        /// <summary>Removes every listener the visualiser added to UnityEvents.</summary>
        private static void DetachAll()
        {
            foreach (var node in nodes.Values)
            {
                foreach (var call in node.Outputs)
                {
                    call.Detach();
                }
            }
        }


        private static NodeData CreateNode(Object entity)
        {
            var id = entity.GetEntityId().GetHashCode();

            NodeData nodeData;

            if (!nodes.TryGetValue(id, out nodeData))
            {
                nodes.Add(id, nodeData = new NodeData(entity));
            }

            return nodeData;
        }
    }
}