using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


// By Harry Rose : https://github.com/Unity-Harry/Unity-AssetDependencyGraph


namespace SOSXR.AssetDependencyGraph
{
    /// <summary>
    /// Visualizes asset dependencies in Unity as an interactive graph view.
    ///
    /// Architecture overview:
    /// - AssetDependencyGraph window hosts a GraphView (AssetGraphView) that renders nodes, edges and groups.
    /// - Nodes represent assets; edges represent dependency relationships between assets.
    /// - A Group node contains all related asset nodes for a single explored asset.
    /// - Internal state:
    ///   - m_GUIDNodeLookup deduplicates assets by GUID to avoid duplicate nodes when the same asset appears
    ///     multiple times in a dependency tree.
    ///   - m_AssetElements tracks graph elements for cleanup when clearing the graph.
    ///   - m_DependenciesForPlacement queues nodes for depth-based automatic layout after their geometry is finalized.
    ///
    /// Graph building algorithm (high level):
    /// 1) Resolve the currently selected asset and its main path. Create a Group node as a container.
    /// 2) Create the main asset node (depth 0). Mark as non-deletable and attach a Select button.
    /// 3) Query direct dependencies (non-recursive) and recursively create dependency nodes per depth.
    /// 4) For each dependency, create edges from the dependency node's input port to the parent node's output port.
    /// 5) Add all nodes and edges to the GraphView and group, and prevent deletions of edges.
    /// 6) Attach a one-time GeometryChangedEvent callback to the main node to trigger auto-layout of dependencies.
    ///
    /// Node/edge lifecycle:
    /// - Nodes are created via CreateNode and deduplicated using m_GUIDNodeLookup.
        /// - Edges are created in CreateDependencyNodes and are marked as non-deletable.
    /// - Nodes/edges are added to m_AssetElements for cleanup when clearing the graph.
    /// - Depth information is stored in node.userData to support depth-based layout in UpdateDependencyNodePlacement().
    ///
    /// Event/callback system:
    /// - The main node registers GeometryChangedEvent to UpdateDependencyNodePlacement, which repositions dependency nodes
    ///   after the main node's geometry has settled. The callback unregisters itself after updating.
    ///
    /// Performance considerations:
    /// - Architecture aims to reduce per-node GeometryChangedEvent registrations (see Agents.md for known issues).
    /// - Current implementation creates nodes/edges on every refresh; consider pooling or lazy-generation in future.
    ///
    /// Public methods and entry points:
    /// - CreateTestGraphViewWindow(): opens the window from the SOSXR menu (Ctrl+Shift+Alt+G).
    /// - ExploreAsset(): builds the graph for the currently selected asset.
    ///
    /// Known issues and improvements:
    /// - See Agents.md for ongoing performance work and planned refactorings (single update cycle, dirty flags, async building).
    /// </summary>
    public class AssetDependencyGraph : EditorWindow
    {
        /// <summary>
        /// Stores all graph elements (nodes, edges, groups) for cleanup and management.
        /// Used to track which elements need to be removed when clearing the graph.
        /// </summary>
        private readonly List<GraphElement> m_AssetElements = new();

        /// <summary>
        /// Maps asset GUIDs to their corresponding graph nodes.
        /// Prevents duplicate node creation for assets that appear multiple times in the dependency tree.
        /// Key: Asset GUID (from AssetDatabase.AssetPathToGUID)
        /// Value: The Node representing that asset in the graph
        /// </summary>
        private readonly Dictionary<string, Node> m_GUIDNodeLookup = new();

        /// <summary>
        /// Tracks nodes that need automatic positioning after the main node's geometry is finalized.
        /// Cleared after UpdateDependencyNodePlacement() completes.
        /// Used to implement depth-based hierarchical layout.
        /// </summary>
        private readonly List<Node> m_DependenciesForPlacement = new();

        /// <summary>
        /// The main GraphView container that displays all nodes and edges.
        /// Initialized in OnEnable() and destroyed in OnDisable().
        /// </summary>
        private GraphView m_GraphView;

        /// <summary>
        /// Standard width for all graph nodes (in pixels).
        /// Used to calculate horizontal spacing between depth levels.
        /// </summary>
        private const float kNodeWidth = 250.0f;


        /// <summary>
        /// Opens the Asset Dependency Graph window from the SOSXR menu.
        /// Keyboard shortcut: Ctrl+Shift+Alt+G
        /// </summary>
        [MenuItem("SOSXR/Asset Dependency/Asset Dependency Graph %#&g")]
        public static void CreateTestGraphViewWindow()
        {
            var window = GetWindow<AssetDependencyGraph>();
            window.titleContent = new GUIContent("Asset Dependency Graph");
        }


        /// <summary>
        /// Initializes the editor window UI when it's first opened or reloaded.
        /// Creates the GraphView, toolbar with buttons and search field, and sets up event handlers.
        /// </summary>
        public void OnEnable()
        {
            // Initialize the main graph view container
            m_GraphView = new AssetGraphView
            {
                name = "Asset Dependency Graph"
            };

            // Create toolbar with dark background
            var toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 0,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.75f)
                }
            };

            // Options container (currently empty, reserved for future options)
            var options = new VisualElement
            {
                style = {alignContent = Align.Center}
            };

            toolbar.Add(options);

            // "Explore Asset" button: Creates graph for the currently selected asset
            toolbar.Add(new Button(ExploreAsset)
            {
                text = "Explore Asset"
            });

            // "Clear" button: Removes all nodes and edges from the graph
            toolbar.Add(new Button(ClearGraph)
            {
                text = "Clear"
            });

            // Search field: Filters and highlights nodes by name
            var ts = new ToolbarSearchField();

            ts.RegisterValueChangedCallback(x =>
            {
                if (string.IsNullOrEmpty(x.newValue))
                {
                    // Empty search: frame all nodes
                    m_GraphView.FrameAll();
                    return;
                }

                m_GraphView.ClearSelection();

                // Search all nodes and select those matching the search term (case-insensitive)
                // Note: Using ToList() due to Unity bug Case 1268337 with direct ForEach on graphElements
                m_GraphView.graphElements.ToList().ForEach(y =>
                {
                    if (y is Node node && y.title.IndexOf(x.newValue, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        m_GraphView.AddToSelection(node);
                    }
                });

                // Frame the selected nodes in view
                m_GraphView.FrameSelection();
            });

            toolbar.Add(ts);

            // Assemble the UI hierarchy
            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(m_GraphView);
            m_GraphView.StretchToParentSize();
            toolbar.BringToFront();
        }


        /// <summary>
        /// Cleans up the editor window when it's closed or reloaded.
        /// Removes the GraphView from the UI hierarchy.
        /// Note: Consider adding callback cleanup here to prevent memory leaks (see Agents.md).
        /// </summary>
        public void OnDisable()
        {
            rootVisualElement.Remove(m_GraphView);
        }


        /// <summary>
        /// Creates a dependency graph for the currently selected asset.
        /// 
        /// Process:
        /// 1. Gets the selected asset from the editor selection
        /// 2. Creates a Group node to contain all related nodes
        /// 3. Creates the main node for the selected asset
        /// 4. Recursively creates nodes for all dependencies
        /// 5. Registers a geometry callback to auto-layout dependency nodes
        /// 
        /// Note: The GeometryChangedEvent callback on mainNode is a known performance bottleneck.
        /// See Agents.md for planned improvements to use a single controlled update instead.
        /// </summary>
        private void ExploreAsset()
        {
            var obj = Selection.activeObject;
            var assetPath = AssetDatabase.GetAssetPath(obj);

            // assetPath will be empty if obj is null or isn't an asset (a scene object)
            if (obj == null || string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            // Create a group node to organize all related nodes
            var groupNode = new Group {title = obj.name};
            var mainObject = AssetDatabase.LoadMainAssetAtPath(assetPath);

            // Get direct dependencies (non-recursive)
            var dependencies = AssetDatabase.GetDependencies(assetPath, false);
            var hasDependencies = dependencies.Length > 0;

            // Create the main node at depth 0
            var mainNode = CreateNode(mainObject, assetPath, true, hasDependencies);

            if (mainNode == null)
            {
                return;
            }

            mainNode.userData = 0;

            // Position main node at origin
            mainNode.SetPosition(new Rect(0, 0, 0, 0));
            m_GraphView.AddElement(groupNode);
            m_GraphView.AddElement(mainNode);

            // Add main node to the group
            groupNode.AddElement(mainNode);

            // Recursively create nodes for all dependencies
            // The visited set stops cyclic references from recursing forever
            var visited = new HashSet<string> {assetPath};
            CreateDependencyNodes(dependencies, mainNode, groupNode, 1, visited);

            // Track elements for cleanup
            m_AssetElements.Add(mainNode);
            m_AssetElements.Add(groupNode);
            groupNode.capabilities &= ~Capabilities.Deletable;

            groupNode.Focus();

            // Register callback to auto-layout dependency nodes once main node geometry is finalized
            // WARNING: This is a performance bottleneck - see Agents.md for planned improvements
            mainNode.RegisterCallback<GeometryChangedEvent>(UpdateDependencyNodePlacement);
        }


        /// <summary>
        /// Recursively creates graph nodes for all dependencies of a given asset.
        /// 
        /// For each dependency:
        /// 1. Loads the asset and its sub-dependencies
        /// 2. Creates or reuses a node (via CreateNode which checks m_GUIDNodeLookup)
        /// 3. Sets the node's depth (stored in userData)
        /// 4. Recursively processes deeper dependencies
        /// 5. Creates an edge connecting the dependency to its parent
        /// 6. Adds the node to the group and tracks it for layout
        /// 
        /// Depth tracking is used for hierarchical positioning in UpdateDependencyNodePlacement().
        /// </summary>
        /// <param name="dependencies">Array of asset paths that are dependencies</param>
        /// <param name="parentNode">The node that depends on these assets</param>
        /// <param name="groupNode">The group to add new nodes to</param>
        /// <param name="depth">Current depth in the dependency tree (0 = main asset)</param>
        /// <param name="visited">Asset paths already expanded. Prevents infinite recursion on cyclic references.</param>
        private void CreateDependencyNodes(string[] dependencies, Node parentNode, Group groupNode, int depth, HashSet<string> visited)
        {
            if (parentNode == null || parentNode.outputContainer.childCount == 0)
            {
                return;
            }

            foreach (var dependencyString in dependencies)
            {
                // Load the dependency asset and its sub-dependencies
                var dependencyAsset = AssetDatabase.LoadMainAssetAtPath(dependencyString);

                if (dependencyAsset == null)
                {
                    continue;
                }

                var deeperDependencies = AssetDatabase.GetDependencies(dependencyString, false);

                // Create or retrieve the node for this dependency
                var dependencyNode = CreateNode(dependencyAsset, AssetDatabase.GetAssetPath(dependencyAsset),
                    false, deeperDependencies.Length > 0);

                // CreateNode returns null for objects that are not persistent assets
                if (dependencyNode == null)
                {
                    continue;
                }

                // Set depth if this is a new node
                if (!m_AssetElements.Contains(dependencyNode))
                {
                    dependencyNode.userData = depth;
                }

                // Recursively process deeper dependencies, once per asset
                if (visited.Add(dependencyString))
                {
                    CreateDependencyNodes(deeperDependencies, dependencyNode, groupNode, depth + 1, visited);
                }

                // A cycle back to the main asset has no input port to connect to
                if (dependencyNode.inputContainer.childCount == 0)
                {
                    continue;
                }

                // Add node to graph view if not already present
                if (!m_GraphView.Contains(dependencyNode))
                {
                    m_GraphView.AddElement(dependencyNode);
                }

                // Create edge from parent to this dependency
                var edge = new Edge
                {
                    input = dependencyNode.inputContainer[0] as Port,
                    output = parentNode.outputContainer[0] as Port
                };

                // Connect the edge to both ports
                edge.input?.Connect(edge);
                edge.output?.Connect(edge);

                dependencyNode.RefreshPorts();
                m_GraphView.AddElement(edge);

                // Add node to group if not already present
                if (!m_AssetElements.Contains(dependencyNode))
                {
                    groupNode.AddElement(dependencyNode);
                }

                // Prevent deletion of edges and track elements
                edge.capabilities &= ~Capabilities.Deletable;
                m_AssetElements.Add(edge);
                m_AssetElements.Add(dependencyNode);

                // Queue node for automatic layout positioning
                if (!m_DependenciesForPlacement.Contains(dependencyNode))
                {
                    m_DependenciesForPlacement.Add(dependencyNode);
                }
            }
        }


        /// <summary>
        /// Creates a new graph node for an asset, or returns an existing one if already created.
        /// 
        /// Deduplication: If the same asset appears multiple times in the dependency tree,
        /// this method returns the existing node and increments its depth counter.
        /// This prevents duplicate nodes and reduces memory usage.
        /// 
        /// Node Features:
        /// - Title: Asset name
        /// - Info Container: Asset path and type information
        /// - Asset Preview: Thumbnail or mini-icon if available
        /// - Ports: Input port for dependencies, output port if has dependencies
        /// - Select Button: Pings and selects the asset in the editor
        /// </summary>
        /// <param name="obj">The asset object to create a node for</param>
        /// <param name="assetPath">The asset's path in the project</param>
        /// <param name="isMainNode">True if this is the root asset being explored</param>
        /// <param name="hasDependencies">True if this asset has dependencies</param>
        /// <returns>A Node representing the asset in the graph</returns>
        private Node CreateNode(Object obj, string assetPath, bool isMainNode, bool hasDependencies)
        {
            Node resultNode = null;

            if (obj == null || string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            var assetGUID = AssetDatabase.AssetPathToGUID(assetPath);

            // Check if we've already created a node for this asset (deduplication)
            if (m_GUIDNodeLookup.TryGetValue(assetGUID, out resultNode))
            {
                // Increment depth counter to track how many times this asset appears
                var currentDepth = (int) resultNode.userData;
                resultNode.userData = currentDepth + 1;

                return resultNode;
            }

            // Create a new node for this asset
            // ReSharper disable once SuggestVarOrType_BuiltInTypes
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var assetGuid, out long _))
            {
                // Create the base node with fixed width
                var objNode = new Node
                {
                    title = obj.name,
                    style =
                    {
                        width = kNodeWidth
                    }
                };

                // Set dark background color for the extension container
                objNode.extensionContainer.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);

                // Add "Select" button to ping and select the asset in the editor
                objNode.titleContainer.Add(new Button(() =>
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                })
                {
                    style =
                    {
                        height = 16.0f,
                        alignSelf = Align.Center,
                        alignItems = Align.Center
                    },
                    text = "Select"
                });

                // Create info container for asset path and type
                var infoContainer = new VisualElement
                {
                    style =
                    {
                        paddingBottom = 4.0f,
                        paddingTop = 4.0f,
                        paddingLeft = 4.0f,
                        paddingRight = 4.0f
                    }
                };

                // Add asset path label
                infoContainer.Add(new Label
                {
                    text = assetPath,
                    style = {whiteSpace = WhiteSpace.Normal}
                });

                // Determine asset type (special handling for prefabs)
                var typeName = obj.GetType().Name;

                if (isMainNode)
                {
                    var prefabType = PrefabUtility.GetPrefabAssetType(obj);

                    if (prefabType != PrefabAssetType.NotAPrefab)
                    {
                        typeName = $"{prefabType} Prefab";
                    }
                }

                // Add type label
                var typeLabel = new Label
                {
                    text = $"Type: {typeName}"
                };

                infoContainer.Add(typeLabel);
                objNode.extensionContainer.Add(infoContainer);

                // Try to load asset preview or mini thumbnail
                Texture assetTexture = AssetPreview.GetAssetPreview(obj);

                if (!assetTexture)
                {
                    assetTexture = AssetPreview.GetMiniThumbnail(obj);
                }

                // Add preview image if available
                if (assetTexture)
                {
                    AddDivider(objNode);

                    objNode.extensionContainer.Add(new Image
                    {
                        image = assetTexture,
                        scaleMode = ScaleMode.ScaleToFit,
                        style =
                        {
                            paddingBottom = 4.0f,
                            paddingTop = 4.0f,
                            paddingLeft = 4.0f,
                            paddingRight = 4.0f
                        }
                    });
                }

                // Add ports for graph connections
                // Input port: for assets that depend on this one (only on non-main nodes)
                if (!isMainNode)
                {
                    var realPort = objNode.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(Object));
                    realPort.portName = "Dependent";
                    objNode.inputContainer.Add(realPort);
                }

                // Output port: for assets this one depends on (only if has dependencies)
                if (hasDependencies)
                {
                    #if UNITY_2018_1
                    // Unity 2018.1 compatibility
                    Port port = objNode.InstantiatePort(Orientation.Horizontal, Direction.Output, typeof(Object));
                    #else
                    var port = objNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Object));
                    #endif
                    port.portName = "Dependencies";
                    objNode.outputContainer.Add(port);
                    objNode.RefreshPorts();
                }

                resultNode = objNode;

                // Finalize node setup
                resultNode.RefreshExpandedState();
                resultNode.RefreshPorts();
                resultNode.capabilities &= ~Capabilities.Deletable;  // Prevent accidental deletion
                resultNode.capabilities |= Capabilities.Collapsible;  // Allow collapsing node contents
            }

            // Cache the node by GUID for deduplication. Never cache null.
            if (resultNode != null)
            {
                m_GUIDNodeLookup[assetGUID] = resultNode;
            }

            return resultNode;
        }


        /// <summary>
        /// Adds a horizontal divider line to a node's extension container.
        /// Used to visually separate the info section from the preview image.
        /// </summary>
        /// <param name="objNode">The node to add the divider to</param>
        private static void AddDivider(Node objNode)
        {
            var divider = new VisualElement {name = "divider"};
            divider.AddToClassList("horizontal");
            objNode.extensionContainer.Add(divider);
        }


        private void ClearGraph()
        {
            foreach (var edge in m_AssetElements)
            {
                m_GraphView.RemoveElement(edge);
            }

            m_AssetElements.Clear();

            foreach (var node in m_AssetElements)
            {
                m_GraphView.RemoveElement(node);
            }

            m_AssetElements.Clear();
            m_GUIDNodeLookup.Clear();
            m_DependenciesForPlacement.Clear();
        }


        private void UpdateDependencyNodePlacement(GeometryChangedEvent e)
        {
            (e.target as VisualElement)?.UnregisterCallback<GeometryChangedEvent>(UpdateDependencyNodePlacement);

            // The current y offset in per depth
            var depthYOffset = new Dictionary<int, float>();

            foreach (var node in m_DependenciesForPlacement)
            {
                var depth = (int) node.userData;

                if (!depthYOffset.ContainsKey(depth))
                {
                    depthYOffset.Add(depth, 0.0f);
                }

                depthYOffset[depth] += node.layout.height;
            }

            // Move half of the node into negative y space so they're on either size of the main node in y axis
            var depths = new List<int>(depthYOffset.Keys);

            foreach (var depth in depths)
            {
                if (depth == 0)
                {
                    continue;
                }

                var offset = depthYOffset[depth];
                depthYOffset[depth] = 0f - offset / 2.0f;
            }

            foreach (var node in m_DependenciesForPlacement)
            {
                var depth = (int) node.userData;
                node.SetPosition(new Rect(kNodeWidth * 1.5f * depth, depthYOffset[depth], 0, 0));
                depthYOffset[depth] += node.layout.height;
            }

            m_DependenciesForPlacement.Clear();
        }
    }
}
