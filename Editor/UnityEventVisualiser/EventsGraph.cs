using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;


namespace EventVisualizer.Base
{
    /// <summary>
    /// Node graph of UnityEvent calls found in the scene. Nodes are laid out automatically after each build.
    /// Create instances with Create. The graph is hidden and not saved to disk.
    /// </summary>
    [Serializable]
    public class EventsGraph : Graph
    {
        private GameObject[] selectedRoots;

        // Object references to scene GameObjects do not survive the domain reload on entering play mode,
        // so the roots are also kept as GlobalObjectId strings and resolved again when needed.
        // Null or empty means "whole scene".
        [SerializeField] private string[] selectedRootIds;
        [SerializeField] private bool searchingHierarchy;
        private const int WARNING_CALLS_THRESOLD = 1000;


        /// <summary>Creates a hidden, unsaved EventsGraph instance.</summary>
        /// <returns>The new graph.</returns>
        public static EventsGraph Create()
        {
            var graph = CreateInstance<EventsGraph>();
            graph.hideFlags = HideFlags.HideAndDontSave;

            return graph;
        }


        /// <summary>Creates a hidden EventsGraphGUI bound to this graph.</summary>
        /// <returns>The new GUI instance.</returns>
        public EventsGraphGUI GetEditor()
        {
            var gui = CreateInstance<EventsGraphGUI>();
            gui.graph = this;
            gui.hideFlags = HideFlags.HideAndDontSave;

            return gui;
        }


        /// <summary>
        /// Rebuilds the graph for the given root GameObjects and lays out the nodes.
        /// The roots are also stored as GlobalObjectId strings so they can be resolved again after a domain reload.
        /// </summary>
        /// <param name="roots">The root GameObjects to search. Null or empty means the whole scene.</param>
        /// <param name="searchHierarchy">True to include the child objects of the roots, false to search only the roots.</param>
        public void RebuildGraph(GameObject[] roots, bool searchHierarchy)
        {
            selectedRoots = roots;
            selectedRootIds = ToGlobalIds(roots);
            searchingHierarchy = searchHierarchy;
            BuildGraph(selectedRoots, searchHierarchy);
            SortGraph(nodes, false);
        }


        /// <summary>
        /// Rebuilds the graph from the last roots and keeps the positions of nodes that still exist.
        /// Resolves the stored root ids again if the roots were lost. New nodes are placed by the sorter.
        /// </summary>
        public void RefreshGraphConnections()
        {
            var positions = new Dictionary<string, Rect>();
            var adriftNodes = new List<Node>();

            foreach (NodeGUI node in nodes)
            {
                positions.Add(node.name, node.position);
            }

            // Lost after a domain reload, or destroyed by a scene reload: resolve the stored ids again
            if (selectedRootIds != null && selectedRootIds.Length > 0 && (selectedRoots == null || selectedRoots.Any(r => r == null)))
            {
                selectedRoots = FromGlobalIds(selectedRootIds);
            }

            BuildGraph(selectedRoots, searchingHierarchy);

            foreach (NodeGUI node in nodes)
            {
                if (positions.ContainsKey(node.name))
                {
                    node.position = positions[node.name];
                }
                else
                {
                    adriftNodes.Add(node);
                }
            }

            SortGraph(adriftNodes, true);
        }


        private static string[] ToGlobalIds(GameObject[] roots)
        {
            if (roots == null || roots.Length == 0)
            {
                return null;
            }

            var ids = new string[roots.Length];

            for (var i = 0; i < roots.Length; i++)
            {
                ids[i] = roots[i] != null ? GlobalObjectId.GetGlobalObjectIdSlow(roots[i]).ToString() : null;
            }

            return ids;
        }


        /// <summary>
        /// Unresolved ids stay as null entries. A non-empty array of nulls finds no events,
        /// which is safer than falling back to a whole-scene scan the user did not ask for.
        /// </summary>
        private static GameObject[] FromGlobalIds(string[] ids)
        {
            var roots = new GameObject[ids.Length];

            for (var i = 0; i < ids.Length; i++)
            {
                if (!string.IsNullOrEmpty(ids[i]) && GlobalObjectId.TryParse(ids[i], out var id))
                {
                    roots[i] = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                }
            }

            return roots;
        }


        private void BuildGraph(GameObject[] roots, bool searchHierarchy)
        {
            NodeData.ClearAll();
            Clear(true);
            var calls = EventsFinder.FindAllEvents(roots, searchHierarchy);

            if (calls.Count > WARNING_CALLS_THRESOLD)
            {
                var goAhead = EditorUtility.DisplayDialog("Confirm massive graph",
                    "You are about to generate a graph with " + calls.Count + " events.\n"
                    + "Tip: You can select some gameobjects and search events in just those or their children instead.",
                    "Go ahead",
                    "Abort");

                if (goAhead)
                {
                    GenerateGraphFromCalls(calls);
                }
            }
            else
            {
                GenerateGraphFromCalls(calls);
            }
        }


        private void GenerateGraphFromCalls(List<EventCall> calls)
        {
            foreach (var call in calls)
            {
                NodeData.RegisterEvent(call);
            }

            foreach (var data in NodeData.Nodes)
            {
                var node = NodeGUI.Create(data);

                if (!nodes.Contains(node))
                {
                    AddNode(node);
                }
            }

            foreach (NodeGUI node in nodes)
            {
                node.PopulateEdges();
            }
        }


        #region sorting

        private HashSet<Node> positionedNodes = new();
        private const float VERTICAL_SPACING = 80f;
        private const float HORIZONTAL_SPACING = 400f;


        private void SortGraph(List<Node> nodes, bool skipParents)
        {
            positionedNodes.Clear();

            var sortedNodes = new List<Node>(nodes); //cannot sort the original collection so a clone is needed

            sortedNodes.Sort((x, y) =>
            {
                var xScore = x.outputEdges.Count() - x.inputEdges.Count();
                var yScore = y.outputEdges.Count() - y.inputEdges.Count();

                return yScore.CompareTo(xScore);
            });

            var position = Vector2.zero;

            foreach (var node in sortedNodes)
            {
                if (!positionedNodes.Contains(node))
                {
                    positionedNodes.Add(node);
                    position.y += PositionNodeHierarchy(node, position, skipParents);
                }
            }
        }


        private float PositionNodeHierarchy(Node currentNode, Vector2 masterPosition, bool skipParents)
        {
            var height = VERTICAL_SPACING;

            if (!skipParents)
            {
                foreach (var outputEdge in currentNode.outputEdges)
                {
                    var node = outputEdge.toSlot.node;

                    if (!positionedNodes.Contains(node))
                    {
                        positionedNodes.Add(node);

                        height += PositionNodeHierarchy(node, masterPosition
                                                              + Vector2.right * HORIZONTAL_SPACING
                                                              + Vector2.up * height, skipParents);
                    }
                }
            }

            currentNode.position = new Rect(masterPosition + Vector2.up * height * 0.5f, currentNode.position.size);

            return height;
        }

        #endregion
    }
}