using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Codelyzer.Analysis
{
    public class CodeGraph
    {
        protected readonly ILogger Logger;
        public HashSet<Node> ProjectGraph { get; set; }
        public HashSet<Node> ClassGraph { get; set; }
        // Nodes are unique
        HashSet<ProjectWorkspace> projectWorkspaces;
        HashSet<string> namespaceDeclarations;
        List<ClassDeclaration> classDeclarations ;
        List<InterfaceDeclaration> interfaceDeclarations ;
        // Edges can have duplicates
        Dictionary<string, List<UstNode>> ustNodesByParent;

        public CodeGraph(ILogger logger)
        {
            Logger = logger;
        }
         
        public void Initialize(List<AnalyzerResult> analyzerResults)
        {
            projectWorkspaces = new HashSet<ProjectWorkspace>();
            namespaceDeclarations = new HashSet<string>();
            classDeclarations = new List<ClassDeclaration>();
            interfaceDeclarations = new List<InterfaceDeclaration>();
            ustNodesByParent = new Dictionary<string, List<UstNode>>();
            ProjectGraph = new HashSet<Node>();
            ClassGraph = new HashSet<Node>(); 
            PopulateGraphs(analyzerResults);
        }
        private void PopulateGraphs(List<AnalyzerResult> analyzerResults)
        {
            analyzerResults.ForEach(analyzerResult =>
            {
                // Add Projects
                projectWorkspaces.Add(analyzerResult.ProjectResult);

                // Add Relevant Children from source files
                analyzerResult.ProjectResult.SourceFileResults.ForEach(sourceFileResult =>
                {
                    InitializeNodesHelper(sourceFileResult);
                });
            });
            RemoveExternalEdges();
            try
            {
                AddProjectNodes();
                AddProjectEdges();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while creating project graph");
            }
            try
            {
                AddNodes();
                AddEdges();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while creating classes graph");
            }
        }
        private void AddProjectNodes()
        {
            projectWorkspaces?.ToList().ForEach(projectResult =>
            {
                try
                {
                    AddProjectNode(projectResult);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while adding project nodes");
                }
            });
        }
        private void AddProjectEdges()
        {
            projectWorkspaces?.ToList().ForEach(projectResult =>
            {
                try { 
                var projectReferences = projectResult?.ExternalReferences?.ProjectReferences;
                var sourceNode = AddProjectNode(projectResult);

                projectReferences?.ForEach(projectReference => {
                    var targetNode = GetProjectNode(projectReference.AssemblyLocation);
                    sourceNode.Edges.Add(new Edge() { EdgeType = EdgeType.ProjectReference, TargetNode = targetNode, SourceNode = sourceNode });
                });
            }catch (Exception ex)
            {
                Logger.LogError(ex, "Error while adding project edges");
            }
        });

        }
        private Node AddProjectNode(ProjectWorkspace projectWorkspace)
        {
            Node node = ProjectGraph.FirstOrDefault(n => n.Identifier == projectWorkspace.ProjectFilePath);
            if (node == null)
            {
                node = new Node()
                {
                    NodeType = NodeType.Project,
                    Name = projectWorkspace.ProjectName,
                    Identifier = projectWorkspace.ProjectFilePath
                };
                ProjectGraph.Add(node);
            }
            return node;
        }
        private Node GetProjectNode(string assemblyLocation) => ProjectGraph.FirstOrDefault(p => p.Identifier == assemblyLocation);
        private void AddNodes()
        {
            classDeclarations.ToList().ForEach(classDeclaration => AddClassNode(classDeclaration));
            interfaceDeclarations.ToList().ForEach(interfaceDeclaration => AddClassNode(interfaceDeclaration));
        }
        private void AddEdges()
        {
            // Add inheritance relations
            classDeclarations.ToList().ForEach(classDeclaration =>
            {
                var sourceNode = GetClassNode(classDeclaration.FullIdentifier);

                // Check base types list for interfaces
                var baseTypes = classDeclaration.BaseList;
                var baseTypeOriginalDefinition = classDeclaration.BaseTypeOriginalDefinition;
                if(!string.IsNullOrEmpty(baseTypeOriginalDefinition) && baseTypeOriginalDefinition != "object")
                {
                    baseTypes.Add(baseTypeOriginalDefinition);
                }
                baseTypes.ForEach(baseType =>
                {
                    var targetNode = GetClassNode(baseType);
                    if (targetNode != null)
                    {
                        var edge = new Edge()
                        {
                            EdgeType = EdgeType.Inheritance,
                            TargetNode = targetNode,
                            SourceNode = sourceNode
                        };
                        sourceNode.Edges.Add(edge);
                        targetNode.Edges.Add(edge);
                    }
                });
            });

            // Add other types of edges
            ustNodesByParent.ToList().ForEach(ustNodeKeyValue =>
            {
                var parentKey = ustNodeKeyValue.Key;
                var sourceNode = GetClassNode(parentKey);

                ustNodeKeyValue.Value.ForEach(childNode =>
                {
                    if (childNode is DeclarationNode)
                    {
                        var targetNode = GetClassNode(childNode.FullIdentifier);
                        if (targetNode?.Equals(sourceNode) == false)
                        {
                            var edge = new Edge()
                            {
                                EdgeType = EdgeType.Declaration,
                                TargetNode = targetNode,
                                SourceNode = sourceNode
                            };
                            sourceNode.Edges.Add(edge);
                            targetNode.Edges.Add(edge);
                        }
                    }
                    else if (childNode is MemberAccess memberAccess)
                    {
                        var targetNode = GetClassNode(memberAccess.SemanticFullClassTypeName);

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
                            sourceNode.Edges.Add(edge);
                            targetNode.Edges.Add(edge);
                        }

                    }
                    else if (childNode is InvocationExpression invocation)
                    {
                        var targetNode = GetClassNode(invocation.SemanticFullClassTypeName);

                        //Skip methods in same class
                        if (targetNode?.Equals(sourceNode) == false)
                        {
                            var edge = new Edge()
                            {
                                EdgeType = invocation is ObjectCreationExpression ? EdgeType.ObjectCreation : EdgeType.Invocation,
                                TargetNode = targetNode,
                                Identifier = invocation.MethodName,
                                SourceNode = sourceNode
                            };
                            sourceNode.Edges.Add(edge);
                            targetNode.Edges.Add(edge);
                        }
                    }
                });
            });
        }
        private Node AddClassNode<T>(T ustNode) where T : UstNode
        {
            Node node = ClassGraph.FirstOrDefault(n => n.Identifier == ustNode.FullIdentifier);
            if (node == null)
            {
                node = new Node()
                {
                    NodeType = GetNodeType(ustNode),
                    Name = ustNode.Identifier,
                    Identifier = ustNode.FullIdentifier,
                    SourceUstNode = ustNode
                };
                ClassGraph.Add(node);
            }
            return node;
        }
        private Node GetClassNode(string identifier) => ClassGraph.FirstOrDefault(c => c.Identifier == identifier);
        private void RemoveExternalEdges()
        {
            ustNodesByParent.Keys.ToList().ForEach(key => {
                ustNodesByParent[key] = ustNodesByParent[key].Where(child => {
                    if (child is InvocationExpression invocation)
                    {
                        return namespaceDeclarations.Contains(invocation.SemanticNamespace); ;
                    }
                    else if (child is DeclarationNode declaration)
                    {
                        return namespaceDeclarations.Contains(declaration?.Reference?.Namespace);
                    }
                    else if (child is MemberAccess memberAccess)
                    {
                        return namespaceDeclarations.Contains(memberAccess?.Reference?.Namespace);
                    }
                    return false;
                }).ToList();
            });
        }
        private NodeType GetNodeType<T>(T ustNode)
        {
            if (ustNode is ClassDeclaration)
            {
                return NodeType.Class;
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
        private void InitializeNodesHelper(UstNode node, string parentNode = null)
        {
            foreach (UstNode child in node.Children)
            {
                if (child != null)
                {
                    try
                    {
                        if (child is NamespaceDeclaration namespaceNode)
                        {
                            if (!namespaceDeclarations.Contains(namespaceNode.Identifier))
                            {
                                namespaceDeclarations.Add(namespaceNode.Identifier);
                            }
                        }
                        else if (child is ClassDeclaration classNode)
                        {
                            classDeclarations.Add(classNode);
                            InitializeNodesHelper(child, child.FullIdentifier);
                        }
                        else if (child is InterfaceDeclaration interfaceNode)
                        {
                            interfaceDeclarations.Add(interfaceNode);
                            InitializeNodesHelper(child, child.FullIdentifier);
                        }
                        else if (child is MemberAccess || child is DeclarationNode || child is InvocationExpression)
                        {
                            GetOrCreateChildren(parentNode)?.Add(child);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error while populating children");
                    }
                    InitializeNodesHelper(child, parentNode);
                }
            }
        }
        private List<UstNode> GetOrCreateChildren(string parentIdentifier)
        {
            // Node is not in a class or an interface, we don't need these tokens
            if (string.IsNullOrEmpty(parentIdentifier))
            {
                return null;
            }

            if (!ustNodesByParent.ContainsKey(parentIdentifier))
            {
                ustNodesByParent.Add(parentIdentifier, new List<UstNode>());
            }
            return ustNodesByParent[parentIdentifier];
        }
    }

    public class Node
    {
        public Node()
        {
            Properties = new Dictionary<string, string>();
            Edges = new List<Edge>();
        }
        public string Name { get; set; }
        public string Identifier { get; set; }
        public NodeType NodeType { get; set; }
        public UstNode SourceUstNode { get; set; }
        public List<Edge> Edges { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public override bool Equals(object obj)
        {
            var node = obj as Node;
            if(node != null)
            {
                return node.Identifier == this.Identifier
                    && node.NodeType == this.NodeType
                    && node.Edges.SequenceEqual(this.Edges);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier, NodeType, Edges);
        }
    }
    public class Edge
    {
        public Edge()
        {
            Properties = new Dictionary<string, string>();
        }
        public Node SourceNode { get; set; }
        public Node TargetNode { get; set; }
        public EdgeType EdgeType { get; set; }
        public string Identifier { get; set; }
        public Dictionary<string, string> Properties { get; set; }

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
        Class,
        Interface,
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
