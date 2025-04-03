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
    public class AssetDependencyGraph : EditorWindow
    {
        private readonly List<GraphElement> m_AssetElements = new();
        private readonly Dictionary<string, Node> m_GUIDNodeLookup = new();
        private readonly List<Node> m_DependenciesForPlacement = new();

        private GraphView m_GraphView;
        private const float kNodeWidth = 250.0f;


        [MenuItem("SOSXR/Asset Dependency/Asset Dependency Graph %#&g")]
        public static void CreateTestGraphViewWindow()
        {
            var window = GetWindow<AssetDependencyGraph>();
            window.titleContent = new GUIContent("Asset Dependency Graph");
        }


        public void OnEnable()
        {
            m_GraphView = new AssetGraphView
            {
                name = "Asset Dependency Graph"
            };

            var toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 0,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.75f)
                }
            };

            var options = new VisualElement
            {
                style = {alignContent = Align.Center}
            };

            toolbar.Add(options);

            toolbar.Add(new Button(ExploreAsset)
            {
                text = "Explore Asset"
            });

            toolbar.Add(new Button(ClearGraph)
            {
                text = "Clear"
            });

            var ts = new ToolbarSearchField();

            ts.RegisterValueChangedCallback(x =>
            {
                if (string.IsNullOrEmpty(x.newValue))
                {
                    m_GraphView.FrameAll();

                    return;
                }

                m_GraphView.ClearSelection();

                // m_GraphView.graphElements.ForEach(y => { // BROKEN, Case 1268337
                m_GraphView.graphElements.ToList().ForEach(y =>
                {
                    if (y is Node node && y.title.IndexOf(x.newValue, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        m_GraphView.AddToSelection(node);
                    }
                });

                m_GraphView.FrameSelection();
            });

            toolbar.Add(ts);


            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(m_GraphView);
            m_GraphView.StretchToParentSize();
            toolbar.BringToFront();
        }


        public void OnDisable()
        {
            rootVisualElement.Remove(m_GraphView);
        }


        private void ExploreAsset()
        {
            var obj = Selection.activeObject;
            var assetPath = AssetDatabase.GetAssetPath(obj);

            // assetPath will be empty if obj is null or isn't an asset (a scene object)
            if (obj == null || string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            var groupNode = new Group {title = obj.name};
            var mainObject = AssetDatabase.LoadMainAssetAtPath(assetPath);

            var dependencies = AssetDatabase.GetDependencies(assetPath, false);
            var hasDependencies = dependencies.Length > 0;

            var mainNode = CreateNode(mainObject, assetPath, true, hasDependencies);
            mainNode.userData = 0;

            mainNode.SetPosition(new Rect(0, 0, 0, 0));
            m_GraphView.AddElement(groupNode);
            m_GraphView.AddElement(mainNode);

            groupNode.AddElement(mainNode);

            CreateDependencyNodes(dependencies, mainNode, groupNode, 1);

            m_AssetElements.Add(mainNode);
            m_AssetElements.Add(groupNode);
            groupNode.capabilities &= ~Capabilities.Deletable;

            groupNode.Focus();

            mainNode.RegisterCallback<GeometryChangedEvent>(UpdateDependencyNodePlacement);
        }


        private void CreateDependencyNodes(string[] dependencies, Node parentNode, Group groupNode, int depth)
        {
            foreach (var dependencyString in dependencies)
            {
                var dependencyAsset = AssetDatabase.LoadMainAssetAtPath(dependencyString);
                var deeperDependencies = AssetDatabase.GetDependencies(dependencyString, false);

                var dependencyNode = CreateNode(dependencyAsset, AssetDatabase.GetAssetPath(dependencyAsset),
                    false, deeperDependencies.Length > 0);

                if (!m_AssetElements.Contains(dependencyNode))
                {
                    dependencyNode.userData = depth;
                }

                CreateDependencyNodes(deeperDependencies, dependencyNode, groupNode, depth + 1);

                if (!m_GraphView.Contains(dependencyNode))
                {
                    m_GraphView.AddElement(dependencyNode);
                }

                var edge = new Edge
                {
                    input = dependencyNode.inputContainer[0] as Port,
                    output = parentNode.outputContainer[0] as Port
                };

                edge.input?.Connect(edge);
                edge.output?.Connect(edge);

                dependencyNode.RefreshPorts();
                m_GraphView.AddElement(edge);

                if (!m_AssetElements.Contains(dependencyNode))
                {
                    groupNode.AddElement(dependencyNode);
                }

                edge.capabilities &= ~Capabilities.Deletable;
                m_AssetElements.Add(edge);
                m_AssetElements.Add(dependencyNode);

                if (!m_DependenciesForPlacement.Contains(dependencyNode))
                {
                    m_DependenciesForPlacement.Add(dependencyNode);
                }
            }
        }


        private Node CreateNode(Object obj, string assetPath, bool isMainNode, bool hasDependencies)
        {
            Node resultNode;
            var assetGUID = AssetDatabase.AssetPathToGUID(assetPath);

            if (m_GUIDNodeLookup.TryGetValue(assetGUID, out resultNode))
            {
                var currentDepth = (int) resultNode.userData;
                resultNode.userData = currentDepth + 1;

                return resultNode;
            }

            // ReSharper disable once SuggestVarOrType_BuiltInTypes
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var assetGuid, out long _))
            {
                var objNode = new Node
                {
                    title = obj.name,
                    style =
                    {
                        width = kNodeWidth
                    }
                };

                objNode.extensionContainer.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);

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

                infoContainer.Add(new Label
                {
                    text = assetPath,

                    style = {whiteSpace = WhiteSpace.Normal}
                });

                var typeName = obj.GetType().Name;

                if (isMainNode)
                {
                    var prefabType = PrefabUtility.GetPrefabAssetType(obj);

                    if (prefabType != PrefabAssetType.NotAPrefab)
                    {
                        typeName = $"{prefabType} Prefab";
                    }
                }

                var typeLabel = new Label
                {
                    text = $"Type: {typeName}"
                };

                infoContainer.Add(typeLabel);

                objNode.extensionContainer.Add(infoContainer);

                Texture assetTexture = AssetPreview.GetAssetPreview(obj);

                if (!assetTexture)
                {
                    assetTexture = AssetPreview.GetMiniThumbnail(obj);
                }

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

                // Ports
                if (!isMainNode)
                {
                    var realPort = objNode.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(Object));
                    realPort.portName = "Dependent";
                    objNode.inputContainer.Add(realPort);
                }

                if (hasDependencies)
                {
                    #if UNITY_2018_1
                Port port = objNode.InstantiatePort(Orientation.Horizontal, Direction.Output, typeof(Object));
                    #else
                    var port = objNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Object));
                    #endif
                    port.portName = "Dependencies";
                    objNode.outputContainer.Add(port);
                    objNode.RefreshPorts();
                }

                resultNode = objNode;

                resultNode.RefreshExpandedState();
                resultNode.RefreshPorts();
                resultNode.capabilities &= ~Capabilities.Deletable;
                resultNode.capabilities |= Capabilities.Collapsible;
            }

            m_GUIDNodeLookup[assetGUID] = resultNode;

            return resultNode;
        }


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