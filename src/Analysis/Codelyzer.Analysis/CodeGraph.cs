using Codelyzer.Analysis.Model;
using Microsoft.Build.Logging.StructuredLogger;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Codelyzer.Analysis
{
    public class ProjectInfo
    {
        public ProjectInfo() { }
        public string Name { get; set; }
        public string Identifier { get; set; }
        public List<string> References { get; set; }
    }
    public class CodeGraphSerialized
    {
        public HashSet<Node> Graph { get; set; }
        public CodeGraphSerialized()
        {

        }
        public CodeGraphSerialized(CodeGraph codeGraph)
        {
            Graph = codeGraph.Graph.Keys.ToHashSet();
            foreach (var item in Graph)
            {
                item.IEdges = new List<Edge>(item.IncomingEdges);
                item.OEdges = new List<Edge>(item.OutgoingEdges);
                item.IncomingEdges = new ConcurrentBag<Edge>();
                item.OutgoingEdges = new ConcurrentBag<Edge>();
                if (codeGraph.ustNodeEdgeCandidates.ContainsKey(item))
                {
                    item.EdgeCandidates.AddRange(codeGraph.ustNodeEdgeCandidates[item]);
                }
            }
        }

        public CodeGraph Deserialize()
        {
            var graph = new CodeGraph(NullLogger.Instance);
            graph.Graph = new ConcurrentDictionary<Node, string>(Graph.ToDictionary(d => d, v => v.Identifier));
            foreach(var item in Graph)
            {
                item.IncomingEdges = new ConcurrentBag<Edge>(item.IEdges);
                item.OutgoingEdges = new ConcurrentBag<Edge>(item.OEdges);
                if (!graph.ustNodeEdgeCandidates.ContainsKey(item))
                {
                    graph.ustNodeEdgeCandidates.TryAdd(item, new ConcurrentBag<UstNode>(item.EdgeCandidates));
                }
                else
                {
                    item.EdgeCandidates.ForEach(edge =>
                    {
                        graph.ustNodeEdgeCandidates[item].Add(edge);
                    });
                }
            }
            return graph;
        }
    }
    public class CodeGraph
    {
        ConcurrentDictionary<Node, string> _projectNodes;
        ConcurrentDictionary<Node, string> _namespaceNodes;
        ConcurrentDictionary<Node, string> _classNodes;
        ConcurrentDictionary<Node, string> _interfaceNodes;
        ConcurrentDictionary<Node, string> _typeNodes;
        ConcurrentDictionary<Node, string> _structNodes;
        ConcurrentDictionary<Node, string> _enumNodes;
        ConcurrentDictionary<Node, string> _recordNodes;
        ConcurrentDictionary<Node, string> _methodNodes;

        [JsonIgnore]
        protected readonly ILogger Logger;
        [JsonIgnore]
        public ConcurrentDictionary<Node, string> Graph { get; set; }
        [JsonIgnore]
        public ConcurrentDictionary<Node, string> ProjectNodes
        {
            get
            {
                if (_projectNodes == null)
                {
                    _projectNodes = new ConcurrentDictionary<Node, string>(Graph.Where(n => n.Key.NodeType == NodeType.Project));
                }
                return _projectNodes;
            }
        }
        [JsonIgnore]
        public ConcurrentDictionary<Node, string> NamespaceNodes
        {
            get
            {
                if (_namespaceNodes == null)
                {
                    _namespaceNodes = new ConcurrentDictionary<Node, string>(Graph.Where(n => n.Key.NodeType == NodeType.Namespace));
                }
                return _namespaceNodes;
            }
        }
        [JsonIgnore]
        public ConcurrentDictionary<Node, string> ClassNodes
        {
            get
            {
                if (_classNodes == null)
                {
                    _classNodes = new ConcurrentDictionary<Node, string>(Graph.Where(n => n.Key.NodeType == NodeType.Class));
                }
                return _classNodes;
            }
        }
        [JsonIgnore]
        public ConcurrentDictionary<Node, string> InterfaceNodes
        {
            get
            {
                if (_interfaceNodes == null)
                {
                    _interfaceNodes = new ConcurrentDictionary<Node, string>(Graph.Where(n => n.Key.NodeType == NodeType.Interface));
                }
                return _interfaceNodes;
            }
        }
        [JsonIgnore]
        public ConcurrentDictionary<Node, string> TypeNodes
        {
            get
            {
                if (_typeNodes == null)
                {
                    _typeNodes = new ConcurrentDictionary<Node, string>(ClassNodes
                        .Union(InterfaceNodes).Union(StructNodes).Union(RecordNodes).Union(EnumNodes));
                }
                return _typeNodes;
            }
        }
        [JsonIgnore]
        public ConcurrentDictionary<Node, string> StructNodes
        {
            get
            {
                if (_structNodes == null)
                {
                    _structNodes = new ConcurrentDictionary<Node, string>(Graph.Where(n => n.Key.NodeType == NodeType.Struct));
                }
                return _structNodes;
            }
        }
        [JsonIgnore]
        public ConcurrentDictionary<Node, string> EnumNodes
        {
            get
            {
                if (_enumNodes == null)
                {
                    _enumNodes = new ConcurrentDictionary<Node, string>(Graph.Where(n => n.Key.NodeType == NodeType.Enum));
                }
                return _enumNodes;
            }
        }
        [JsonIgnore]
        public ConcurrentDictionary<Node, string> RecordNodes
        {
            get
            {
                if (_recordNodes == null)
                {
                    _recordNodes = new ConcurrentDictionary<Node, string>(Graph.Where(n => n.Key.NodeType == NodeType.Record));
                }
                return _recordNodes;
            }
        }
        [JsonIgnore]
        public ConcurrentDictionary<Node, string> MethodNodes
        {
            get
            {
                if (_methodNodes == null)
                {
                    _methodNodes = new ConcurrentDictionary<Node, string>(Graph.Where(n => n.Key.NodeType == NodeType.Method));
                }
                return _methodNodes;
            }
        }
        ConcurrentBag<ProjectInfo> projectWorkspaces;
        [JsonIgnore]
        // Edges can have duplicates
        public ConcurrentDictionary<Node, ConcurrentBag<UstNode>> ustNodeEdgeCandidates;
        [JsonIgnore]
        public ConcurrentDictionary<Node, ConcurrentBag<UstNode>> filteredUstNodeEdgeCandidates;

        public CodeGraph(ILogger logger)
        {
            Logger = logger;
            projectWorkspaces = new ConcurrentBag<ProjectInfo>();
            ustNodeEdgeCandidates = new ConcurrentDictionary<Node, ConcurrentBag<UstNode>>();
            filteredUstNodeEdgeCandidates = new ConcurrentDictionary<Node, ConcurrentBag<UstNode>>();
            Graph = new ConcurrentDictionary<Node, string>();
        }
        public void Initialize(List<AnalyzerResult> analyzerResults)
        {
            PopulateGraphs(analyzerResults);
        }
        public void MergeGraphs(List<CodeGraph> codeGraphs)
        {
            //Clear previous variable to re-initiate:
            _projectNodes = null;
            _namespaceNodes = null;
            _classNodes = null;
            _interfaceNodes = null;
            _structNodes = null;
            _enumNodes = null;
            _recordNodes = null;
            _methodNodes = null;
            _typeNodes = null;

            var concurrentGraphs = new ConcurrentBag<CodeGraph>(codeGraphs);
            Parallel.ForEach(codeGraphs, codeGraph =>
            {
                foreach (var projectWorkspace in codeGraph.projectWorkspaces)
                {
                    projectWorkspaces.Add(projectWorkspace);
                }
                foreach (var g in codeGraph.Graph)
                {
                    // Should we limit this to namespace only?
                    if (!Graph.ContainsKey(g.Key))
                    {
                        Graph.TryAdd(g.Key, g.Value);
                    }
                    else
                    {
                        // Merge the nodes
                        var existingKey = Graph.FirstOrDefault(g1 => g1.Key.Equals(g.Key));
                        g.Key.ChildNodes?.ToList().ForEach(childNode =>
                        {
                            existingKey.Key.ChildNodes.Add(childNode);
                        });
                        g.Key.IncomingEdges?.ToList().ForEach(incomingEdge =>
                        {
                            existingKey.Key.IncomingEdges.Add(incomingEdge);
                        });
                        g.Key.OutgoingEdges?.ToList().ForEach(outgoingEdge =>
                        {
                            existingKey.Key.OutgoingEdges.Add(outgoingEdge);
                        });
                    }
                }
                foreach (var kvp in codeGraph.ustNodeEdgeCandidates)
                {
                    if (ustNodeEdgeCandidates.ContainsKey(kvp.Key))
                    {
                        foreach (var v in kvp.Value)
                        {
                            ustNodeEdgeCandidates[kvp.Key].Add(v);
                        }
                    }
                    else
                    {
                        ustNodeEdgeCandidates.TryAdd(kvp.Key, kvp.Value);
                    }
                }

            });

            // Remove edges that are external to the projects
            RemoveExternalEdges();

            AddEdges();
        }

        private void PopulateGraphs(List<AnalyzerResult> analyzerResults)
        {
            try
            {
                AddNodes(analyzerResults);
                RemoveExternalEdges();
                AddEdges();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while creating classes graph");
            }
        }
        private void AddNodes(List<AnalyzerResult> analyzerResults)
        {
            analyzerResults.ForEach(analyzerResult =>
            {
                try
                {
                    var projectNode = new Node()
                    {
                        NodeType = NodeType.Project,
                        Name = analyzerResult.ProjectResult.ProjectName,
                        Identifier = analyzerResult.ProjectResult.ProjectFilePath.ToLower()
                    };

                    Graph.TryAdd(projectNode, projectNode.Identifier);
                    projectWorkspaces.Add(new ProjectInfo()
                    {
                        Name = analyzerResult.ProjectResult.ProjectName,
                        Identifier = analyzerResult.ProjectResult.ProjectFilePath,
                        References = analyzerResult.ProjectResult.ExternalReferences.ProjectReferences.Select(r => r.AssemblyLocation).ToList()
                    });

                    // Add Relevant Children from source files
                    analyzerResult.ProjectResult.SourceFileResults.ForEach(sourceFileResult =>
                    {
                        var children = InitializeNodesHelper(sourceFileResult, projectNode);
                        foreach (var child in children)
                        {
                            if (!projectNode.ChildNodes.Contains(child))
                            {
                                projectNode.ChildNodes.Add(child);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error while adding nodes for {analyzerResult.ProjectResult.ProjectFilePath}");
                }
            });
        }
        private void AddEdges()
        {
            AddProjectEdges();
            Parallel.ForEach(ClassNodes, classNode =>
            {
                var ustNode = classNode.Key.UstNode as ClassDeclaration;
                CreateClassHierarchyEdges(classNode.Key, ustNode.BaseList, ustNode.BaseTypeOriginalDefinition);
            });
            Parallel.ForEach(InterfaceNodes, interfaceNode =>
            {
                var ustNode = interfaceNode.Key.UstNode as InterfaceDeclaration;
                CreateClassHierarchyEdges(interfaceNode.Key, ustNode.BaseList, ustNode.BaseTypeOriginalDefinition);
            });
            Parallel.ForEach(StructNodes, structNode =>
            {
                var ustNode = structNode.Key.UstNode as StructDeclaration;
                CreateClassHierarchyEdges(structNode.Key, ustNode.BaseList, ustNode.BaseTypeOriginalDefinition);
            });
            Parallel.ForEach(RecordNodes, recordNode =>
            {
                var ustNode = recordNode.Key.UstNode as RecordDeclaration;
                CreateClassHierarchyEdges(recordNode.Key, ustNode.BaseList, ustNode.BaseTypeOriginalDefinition);
            });
            Parallel.ForEach(filteredUstNodeEdgeCandidates.Keys, key => { CreateEdges(key); });
        }
        private void AddProjectEdges()
        {
            projectWorkspaces?.ToList().ForEach(projectResult =>
            {
                try
                {
                    var projectReferences = projectResult.References;
                    var sourceNode = ProjectNodes.FirstOrDefault(p => p.Key.Identifier.Equals(projectResult.Identifier, StringComparison.InvariantCultureIgnoreCase)).Key;

                    projectReferences?.ForEach(projectReference =>
                    {
                        var targetNode = ProjectNodes.FirstOrDefault(p => p.Key.Identifier.Equals(projectReference, StringComparison.InvariantCultureIgnoreCase)).Key;
                                                
                        if (targetNode != null)
                        {
                            var edge = new Edge() { EdgeType = EdgeType.ProjectReference, TargetNode = targetNode, SourceNode = sourceNode };
                            if (!sourceNode.OutgoingEdges.Contains(edge))
                            {
                                sourceNode.OutgoingEdges.Add(edge);
                                targetNode.IncomingEdges.Add(edge);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error while adding project edges for {projectResult.Identifier}");
                }
            });
        }
        private void RemoveExternalEdges()
        {
            var uniqueNamespaces = Graph.Where(n => n.Key.NodeType == NodeType.Namespace).Select(n => n.Key.Identifier).Distinct().ToHashSet();
            Parallel.ForEach(ustNodeEdgeCandidates, nodeAndChildren => {
                try
                {
                    var key = nodeAndChildren.Key;
                    var value = new ConcurrentBag<UstNode>(nodeAndChildren.Value.Where(child =>
                    {
                        if (child is InvocationExpression invocation)
                        {
                            return uniqueNamespaces.Contains(invocation.SemanticNamespace); ;
                        }
                        else if (child is DeclarationNode declaration)
                        {
                            return uniqueNamespaces.Contains(declaration?.Reference?.Namespace);
                        }
                        else if (child is MemberAccess memberAccess)
                        {
                            return uniqueNamespaces.Contains(memberAccess?.Reference?.Namespace);
                        }
                        return false;
                    }));
                    if (value.Count > 0)
                    {
                        filteredUstNodeEdgeCandidates.TryAdd(key, value);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while removing external edges");
                }
            });
        }
        private NodeType GetNodeType<T>(T ustNode)
        {
            if (ustNode is NamespaceDeclaration)
            {
                return NodeType.Namespace;
            }
            else if (ustNode is ClassDeclaration)
            {
                return NodeType.Class;
            }
            else if (ustNode is StructDeclaration)
            {
                return NodeType.Struct;
            }
            else if (ustNode is EnumDeclaration)
            {
                return NodeType.Enum;
            }
            else if (ustNode is RecordDeclaration)
            {
                return NodeType.Record;
            }
            else if (ustNode is InterfaceDeclaration)
            {
                return NodeType.Interface;
            }
            else if (ustNode is MethodDeclaration)
            {
                return NodeType.Method;
            }
            return NodeType.Unknown;
        }
        private HashSet<Node> InitializeNodesHelper(UstNode node, Node parentNode)
        {
            var childNodes = new HashSet<Node>();
            foreach (UstNode child in node.Children)
            {
                if (child != null)
                {
                    var currentNode = new Node();
                    currentNode.ParentNode = parentNode;
                    currentNode.UstNode = child;
                    try
                    {
                        // These children are the node objects
                        if (IsNode(child))
                        {
                            currentNode.NodeType = GetNodeType(child);
                            currentNode.Name = child.Identifier;
                            currentNode.Identifier = child.FullIdentifier;
                            if (!Graph.Keys.Contains(currentNode))
                            {
                                childNodes.Add(currentNode);
                                Graph.TryAdd(currentNode, currentNode.Identifier);
                            }
                            else
                            {
                                currentNode = Graph.Keys.FirstOrDefault(n => n.Equals(currentNode));
                            }
                            var children = InitializeNodesHelper(child, currentNode);
                            children.ToList().ForEach(child => currentNode.ChildNodes.Add(child));
                        }
                        else
                        {
                            // These children are the edges
                            if (IsEdgeConnection(child))
                            {
                                GetOrAddEdgeCandidates(parentNode)?.Add(child);
                            }
                            // Keep going until there are no more children
                            childNodes = childNodes.Union(InitializeNodesHelper(child, parentNode)).ToHashSet();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error while populating children");
                    }
                }
            }
            return childNodes;
        }
        private void CreateClassHierarchyEdges(Node sourceNode, List<string> baseTypes, string baseTypeOriginalDefinition)
        {
            if (!string.IsNullOrEmpty(baseTypeOriginalDefinition) && baseTypeOriginalDefinition != "object")
            {
                baseTypes.Add(baseTypeOriginalDefinition);
            }
            baseTypes.ForEach(baseType =>
            {
                //If edge is already added, we dont need to proceed
                var existingEdge = sourceNode.OutgoingEdges.FirstOrDefault(e => e.TargetNode.Identifier == baseType);
                if (existingEdge == null)
                {
                    var targetNode = TypeNodes.Keys.FirstOrDefault(n => n.Identifier == baseType);
                    if (targetNode != null)
                    {
                        var edge = new Edge()
                        {
                            EdgeType = EdgeType.Inheritance,
                            TargetNode = targetNode,
                            SourceNode = sourceNode
                        };
                        sourceNode.OutgoingEdges.Add(edge);
                        targetNode.IncomingEdges.Add(edge);
                    }
                }
            });
        }
        private void CreateEdges(Node sourceNode)
        {
            var edgeCandidates = filteredUstNodeEdgeCandidates[sourceNode].ToList();
            Parallel.ForEach(edgeCandidates, edgeCandidate => {
                //If edge is already added, we dont need to proceed
                var existingEdge = sourceNode.OutgoingEdges.FirstOrDefault(e => e.TargetNode.Identifier == edgeCandidate.FullIdentifier);

                if (edgeCandidate is DeclarationNode)
                {
                    if (existingEdge?.EdgeType != EdgeType.Declaration)
                    {
                        var targetNode = TypeNodes.Keys.FirstOrDefault(c => c.Identifier == edgeCandidate.FullIdentifier);
                        if (targetNode?.Equals(sourceNode) == false)
                        {
                            var edge = new Edge()
                            {
                                EdgeType = EdgeType.Declaration,
                                TargetNode = targetNode,
                                SourceNode = sourceNode
                            };
                            sourceNode.OutgoingEdges.Add(edge);
                            targetNode.IncomingEdges.Add(edge);
                        }
                    }
                }
                else if (edgeCandidate is MemberAccess memberAccess)
                {
                    if (existingEdge?.EdgeType != EdgeType.MemberAccess)
                    {
                        var targetNode = TypeNodes.Keys.FirstOrDefault(c => c.Identifier == memberAccess.SemanticFullClassTypeName);

                        //Skip methods in same class
                        if (targetNode?.Equals(sourceNode) == false)
                        {
                            var edge = new Edge()
                            {
                                EdgeType = EdgeType.MemberAccess,
                                TargetNode = targetNode,
                                Identifier = memberAccess.Identifier,
                                SourceNode = sourceNode
                            };
                            sourceNode.OutgoingEdges.Add(edge);
                            targetNode.IncomingEdges.Add(edge);
                        }
                    }
                }
                else if (edgeCandidate is InvocationExpression invocation)
                {
                    if (invocation is ObjectCreationExpression objectCreationExpression)
                    {
                        if (existingEdge?.EdgeType != EdgeType.ObjectCreation)
                        {
                            // Find any constructors with the same signature
                            var targetNode = MethodNodes.Keys.FirstOrDefault(n => n.Identifier == edgeCandidate.FullIdentifier);

                            // No constructors found, find the class type
                            if (targetNode is null)
                            {
                                targetNode = TypeNodes.Keys.FirstOrDefault(n => n.Identifier == objectCreationExpression.SemanticFullClassTypeName);
                            }

                            //Skip methods in same class
                            if (targetNode?.Equals(sourceNode) == false)
                            {
                                var edge = new Edge()
                                {
                                    EdgeType = EdgeType.ObjectCreation,
                                    TargetNode = targetNode,
                                    Identifier = invocation.MethodName,
                                    SourceNode = sourceNode
                                };
                                sourceNode.OutgoingEdges.Add(edge);
                                targetNode.IncomingEdges.Add(edge);
                            }
                        }
                    }
                    else
                    {
                        if (existingEdge?.EdgeType != EdgeType.Invocation)
                        {
                            var targetNode = MethodNodes.Keys.FirstOrDefault(n => n.Identifier == edgeCandidate.FullIdentifier);
                            //Skip methods in same class
                            if (targetNode?.Equals(sourceNode) == false)
                            {
                                var edge = new Edge()
                                {
                                    EdgeType = EdgeType.Invocation,
                                    TargetNode = targetNode,
                                    Identifier = invocation.MethodName,
                                    SourceNode = sourceNode
                                };
                                sourceNode.OutgoingEdges.Add(edge);
                                targetNode.IncomingEdges.Add(edge);
                            }
                        }
                    }
                }
            });
        }
        private ConcurrentBag<UstNode> GetOrAddEdgeCandidates(Node parentNode)
        {
            if (!ustNodeEdgeCandidates.ContainsKey(parentNode))
            {
                ustNodeEdgeCandidates.TryAdd(parentNode, new ConcurrentBag<UstNode>());
            }
            return ustNodeEdgeCandidates[parentNode];
        }
        private bool IsNode(UstNode ustNode) => (ustNode is NamespaceDeclaration || ustNode is ClassDeclaration || ustNode is InterfaceDeclaration
            || ustNode is StructDeclaration || ustNode is EnumDeclaration || ustNode is RecordDeclaration || ustNode is MethodDeclaration);
        private bool IsEdgeConnection(UstNode ustNode) => (ustNode is DeclarationNode || ustNode is MemberAccess || ustNode is InvocationExpression);
    }

    public class Node
    {
        public Node()
        {
            Properties = new Dictionary<string, object>();
            OutgoingEdges = new ConcurrentBag<Edge>();
            IncomingEdges = new ConcurrentBag<Edge>();
            ChildNodes = new HashSet<Node>();
            EdgeCandidates = new List<UstNode>();
        }
        public Node ParentNode { get; set; }
        public HashSet<Node> ChildNodes { get; set; }
        public string Name { get; set; }
        public string Identifier { get; set; }
        public NodeType NodeType { get; set; }
        public List<Edge> IEdges { get; set; }
        public List<Edge> OEdges { get; set; }

        [JsonIgnore]
        public ConcurrentBag<Edge> Edges
        {
            get
            {
                return new ConcurrentBag<Edge>(IncomingEdges.Union(OutgoingEdges));
            }
        }
        [JsonIgnore]
        public ConcurrentBag<Edge> AllEdges
        {
            get
            {
                return new ConcurrentBag<Edge>(AllIncomingEdges.Union(AllOutgoingEdges));
            }
        }
        [JsonIgnore]
        public ConcurrentBag<Edge> OutgoingEdges { get; set; }
        [JsonIgnore]
        public ConcurrentBag<Edge> IncomingEdges { get; set; }
        [JsonIgnore]
        public ConcurrentBag<Edge> AllOutgoingEdges { get => new ConcurrentBag<Edge>(OutgoingEdges.Union(ChildNodes.SelectMany(c => c.AllOutgoingEdges))); }
        [JsonIgnore]
        public ConcurrentBag<Edge> AllIncomingEdges { get => new ConcurrentBag<Edge>(IncomingEdges.Union(ChildNodes.SelectMany(c => c.AllIncomingEdges))); }
        public UstNode UstNode { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public List<UstNode> EdgeCandidates { get;set; }
        public override bool Equals(object obj)
        {
            var node = obj as Node;
            if (node != null)
            {
                return node.Identifier == this.Identifier
                    && node.NodeType == this.NodeType;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier, NodeType);
        }
    }
    public class Edge
    {
        public Edge()
        {
            Properties = new Dictionary<string, object>();
        }
        public Node SourceNode { get; set; }
        public Node TargetNode { get; set; }
        public EdgeType EdgeType { get; set; }
        public string Identifier { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public override bool Equals(object obj)
        {
            var edge = obj as Edge;
            if (edge != null)
            {
                return edge.Identifier == this.Identifier
                    && edge.EdgeType == this.EdgeType
                    && edge.SourceNode == this.SourceNode
                    && edge.TargetNode == this.TargetNode;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier, SourceNode, TargetNode, EdgeType);
        }
    }

    public enum NodeType
    {
        Project,
        Namespace,
        Class,
        Interface,
        Enum,
        Struct,
        Record,
        Method,
        Unknown
    }
    public enum EdgeType
    {
        ProjectReference,
        Inheritance,
        Invocation,
        ObjectCreation,
        Declaration,
        MemberAccess,
        Interface
    }
}
