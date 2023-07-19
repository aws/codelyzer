using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Codelyzer.Analysis
{
    public class CodeGraph
    {
        HashSet<Node> _projectNodes;
        HashSet<Node> _namespaceNodes;
        HashSet<Node> _classNodes;
        HashSet<Node> _interfaceNodes;
        HashSet<Node> _structNodes;
        HashSet<Node> _enumNodes;
        HashSet<Node> _recordNodes;
        HashSet<Node> _methodNodes;

        protected readonly ILogger Logger;
        public HashSet<Node> Graph { get; set; }
        public HashSet<Node> ProjectNodes
        {
            get
            {
                if (_projectNodes == null)
                {
                    _projectNodes = Graph.Where(n => n.NodeType == NodeType.Project).ToHashSet();
                }
                return _projectNodes;
            }
        }
        public HashSet<Node> NamespaceNodes
        {
            get
            {
                if (_namespaceNodes == null)
                {
                    _namespaceNodes = Graph.Where(n => n.NodeType == NodeType.Namespace).ToHashSet();
                }
                return _namespaceNodes;
            }
        }
        public HashSet<Node> ClassNodes
        {
            get
            {
                if (_classNodes == null)
                {
                    _classNodes = Graph.Where(n => n.NodeType == NodeType.Class).ToHashSet();
                }
                return _classNodes;
            }
        }
        public HashSet<Node> InterfaceNodes
        {
            get
            {
                if (_interfaceNodes == null)
                {
                    _interfaceNodes = Graph.Where(n => n.NodeType == NodeType.Interface).ToHashSet();
                }
                return _interfaceNodes;
            }
        }
        public HashSet<Node> StructNodes
        {
            get
            {
                if (_structNodes == null)
                {
                    _structNodes = Graph.Where(n => n.NodeType == NodeType.Struct).ToHashSet();
                }
                return _structNodes;
            }
        }
        public HashSet<Node> TypeNodes
        {
            get
            {
                return ClassNodes.Union(InterfaceNodes).Union(StructNodes).Union(EnumNodes).Union(RecordNodes).ToHashSet();
            }
        }
        public HashSet<Node> EnumNodes
        {
            get
            {
                if (_enumNodes == null)
                {
                    _enumNodes = Graph.Where(n => n.NodeType == NodeType.Enum).ToHashSet();
                }
                return _enumNodes;
            }
        }
        public HashSet<Node> RecordNodes
        {
            get
            {
                if (_recordNodes == null)
                {
                    _recordNodes = Graph.Where(n => n.NodeType == NodeType.Record).ToHashSet();
                }
                return _recordNodes;
            }
        }
        public HashSet<Node> MethodNodes
        {
            get
            {
                if (_methodNodes == null)
                {
                    _methodNodes = Graph.Where(n => n.NodeType == NodeType.Method).ToHashSet();
                }
                return _methodNodes;
            }
        }
        HashSet<ProjectWorkspace> projectWorkspaces;
        // Edges can have duplicates
        Dictionary<Node, List<UstNode>> ustNodeEdgeCandidates;
        Dictionary<Node, List<UstNode>> filteredUstNodeEdgeCandidates;

        public CodeGraph(ILogger logger)
        {
            Logger = logger;
            projectWorkspaces = new HashSet<ProjectWorkspace>();
            ustNodeEdgeCandidates = new Dictionary<Node, List<UstNode>>();
            filteredUstNodeEdgeCandidates = new Dictionary<Node, List<UstNode>>();
            Graph = new HashSet<Node>();
        }         
        public void Initialize(List<AnalyzerResult> analyzerResults)
        {
            PopulateGraphs(analyzerResults);
        }
        public void MergeGraphs(List<CodeGraph> codeGraphs)
        {
            //Clear previous variable to re-initiate:
            _projectNodes=null;
            _namespaceNodes=null;
            _classNodes=null;
            _interfaceNodes=null;
            _structNodes=null;
            _enumNodes=null;
            _recordNodes=null;
            _methodNodes=null;

            foreach (CodeGraph codeGraph in codeGraphs)
            {
                projectWorkspaces.AddRange(codeGraph.projectWorkspaces);
                Graph.AddRange(codeGraph.Graph);
                foreach (var kvp in codeGraph.ustNodeEdgeCandidates)
                {
                    if (ustNodeEdgeCandidates.ContainsKey(kvp.Key))
                    {
                        ustNodeEdgeCandidates[kvp.Key].AddRange(kvp.Value);
                    }
                    else
                    {
                        ustNodeEdgeCandidates.Add(kvp.Key, kvp.Value);
                    }
                }
            }

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

                    Graph.Add(projectNode);
                    projectWorkspaces.Add(analyzerResult.ProjectResult);

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
            ClassNodes.ToList().ForEach(classNode => { CreateClassHierarchyEdges(classNode); });
            InterfaceNodes.ToList().ForEach(interfaceNode => { CreateClassHierarchyEdges(interfaceNode); });
            StructNodes.ToList().ForEach(structNode => { CreateClassHierarchyEdges(structNode); });
            RecordNodes.ToList().ForEach(recordNode => { CreateClassHierarchyEdges(recordNode); });
            filteredUstNodeEdgeCandidates.Keys.ToList().ForEach(key => CreateEdges(key));
        }
        private void AddProjectEdges()
        {
            projectWorkspaces?.ToList().ForEach(projectResult =>
            {
                try
                {
                    var projectReferences = projectResult?.ExternalReferences?.ProjectReferences;
                    var sourceNode = ProjectNodes.FirstOrDefault(p => p.Identifier.Equals(projectResult.ProjectFilePath, StringComparison.InvariantCultureIgnoreCase));

                    projectReferences?.ForEach(projectReference =>
                    {
                        var targetNode = ProjectNodes.FirstOrDefault(p => p.Identifier.Equals(projectReference.AssemblyLocation, StringComparison.InvariantCultureIgnoreCase));
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
                    Logger.LogError(ex, $"Error while adding project edges for {projectResult.ProjectFilePath}");
                }
            });
        }
        private void RemoveExternalEdges()
        {
            var uniqueNamespaces = Graph.Where(n => n.NodeType == NodeType.Namespace).Select(n=>n.Identifier).Distinct().ToHashSet();

            ustNodeEdgeCandidates.ToList().ForEach(nodeAndChildren =>
            {
                try
                {
                    var key = nodeAndChildren.Key;
                    var value = nodeAndChildren.Value.Where(child =>
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
                    }).ToList();
                    if (value.Count > 0)
                    {
                        filteredUstNodeEdgeCandidates.Add(key, value);
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
                            if (!Graph.Contains(currentNode))
                            {
                                childNodes.Add(currentNode);
                                Graph.Add(currentNode);
                            }
                            else
                            {
                                currentNode = Graph.FirstOrDefault(n => n.Equals(currentNode));
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
        private void CreateClassHierarchyEdges(Node sourceNode)
        {
            // TODO - Create a common Declaration class for classes and interfaces:

            var targetNodes = ClassNodes.Union(InterfaceNodes).ToList();

            var baseTypes = new List<string>();
            var baseTypeOriginalDefinition = string.Empty;

            if (sourceNode.UstNode is ClassDeclaration classDeclaration)
            {
                // Check base types list for interfaces
                baseTypes = classDeclaration.BaseList;
                baseTypeOriginalDefinition = classDeclaration.BaseTypeOriginalDefinition;
            }
            else if (sourceNode.UstNode is InterfaceDeclaration interfaceDeclaration)
            {
                // Check base types list for interfaces
                baseTypes = interfaceDeclaration.BaseList;
                baseTypeOriginalDefinition = interfaceDeclaration.BaseTypeOriginalDefinition;
            }
            else if (sourceNode.UstNode is StructDeclaration structDeclaration)
            {
                // Check base types list for interfaces
                baseTypes = structDeclaration.BaseList;
                baseTypeOriginalDefinition = structDeclaration.BaseTypeOriginalDefinition;
            }
            else if (sourceNode.UstNode is RecordDeclaration recordDeclaration)
            {
                // Check base types list for interfaces
                baseTypes = recordDeclaration.BaseList;
                baseTypeOriginalDefinition = recordDeclaration.BaseTypeOriginalDefinition;
            }
            else
            {
                // If it's neither, no need to continue
                return;
            }

            if (!string.IsNullOrEmpty(baseTypeOriginalDefinition) && baseTypeOriginalDefinition != "object")
            {
                baseTypes.Add(baseTypeOriginalDefinition);
            }
            baseTypes.ForEach(baseType =>
            {
                var targetNode = targetNodes.FirstOrDefault(n => n.Identifier == baseType);
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
            });
        }
        private void CreateEdges(Node sourceNode)
        {
            var edgeCandidates = filteredUstNodeEdgeCandidates[sourceNode];

            edgeCandidates.ForEach(edgeCandidate =>
            {
                //If edge is already added, we dont need to proceed
                var existingEdge = sourceNode.OutgoingEdges.FirstOrDefault(e => e.TargetNode.Identifier == edgeCandidate.FullIdentifier);

                if (edgeCandidate is DeclarationNode)
                {
                    if (existingEdge?.EdgeType != EdgeType.Declaration)
                    {
                        var targetNode = TypeNodes.FirstOrDefault(c => c.Identifier == edgeCandidate.FullIdentifier);
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
                        var targetNode = TypeNodes.FirstOrDefault(c => c.Identifier == memberAccess.SemanticFullClassTypeName);

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
                            var targetNode = MethodNodes.FirstOrDefault(n => n.Identifier == edgeCandidate.FullIdentifier);

                            // No constructors found, find the class type
                            if (targetNode is null)
                            {
                                targetNode = TypeNodes.FirstOrDefault(n => n.Identifier == objectCreationExpression.SemanticFullClassTypeName);
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
                            var targetNode = MethodNodes.FirstOrDefault(n => n.Identifier == edgeCandidate.FullIdentifier);
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
        private List<UstNode> GetOrAddEdgeCandidates(Node parentNode)
        {
            if (!ustNodeEdgeCandidates.ContainsKey(parentNode))
            {
                ustNodeEdgeCandidates.Add(parentNode, new List<UstNode>());
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
            OutgoingEdges = new List<Edge>();
            IncomingEdges = new List<Edge>();
            ChildNodes = new HashSet<Node>();
        }
        public Node ParentNode { get; set; }
        public HashSet<Node> ChildNodes { get; set; }
        public string Name { get; set; }
        public string Identifier { get; set; }
        public NodeType NodeType { get; set; }
        public List<Edge> Edges
        {
            get
            {
                return IncomingEdges.Union(OutgoingEdges).ToList();
            }
        }
        public List<Edge> AllEdges
        {
            get
            {
                return AllIncomingEdges.Union(AllOutgoingEdges).ToList();
            }
        }
        public List<Edge> OutgoingEdges { get; set; }
        public List<Edge> IncomingEdges { get; set; }
        public List<Edge> AllOutgoingEdges { get => OutgoingEdges.Union(ChildNodes.SelectMany(c => c.AllOutgoingEdges)).ToList(); }
        public List<Edge> AllIncomingEdges { get => IncomingEdges.Union(ChildNodes.SelectMany(c => c.AllIncomingEdges)).ToList(); }
        public UstNode UstNode { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public override bool Equals(object obj)
        {
            var node = obj as Node;
            if(node != null)
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
            if(edge != null)
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
