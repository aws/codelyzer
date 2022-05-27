using Codelyzer.Analysis.Model;
using System.Collections.Generic;
using System.Linq;

namespace Codelyzer.Analysis
{
    public class CodeGraph
    {
        public List<Node> ProjectGraph { get; set; }
        public List<Node> ClassGraph { get; set; }

        public void Initialize(List<AnalyzerResult> analyzerResults)
        {
            ProjectGraph = CreateProjectGraph(analyzerResults);
            ClassGraph = CreateClassesGraph(analyzerResults);
        }
        private List<Node> CreateProjectGraph(List<AnalyzerResult> analyzerResults)
        {
            var graph = new List<Node>();

            // Add Nodes
            foreach (var analyzerResult in analyzerResults)
            {
                var projectResult = analyzerResult.ProjectResult;
                var projectNode = new Node() { NodeType = NodeType.Project, Name = projectResult.ProjectName, Identifier = projectResult.ProjectFilePath };
                graph.Add(projectNode);
            }

            // Add Edges
            foreach (var node in graph)
            {
                var project = analyzerResults.FirstOrDefault(a => a.ProjectResult.ProjectFilePath == node.Identifier);
                var projectReferences = project.ProjectResult?.ExternalReferences?.ProjectReferences;

                projectReferences?.ForEach(projectReference => {
                    var targetNode = graph.FirstOrDefault(n => n.Identifier == projectReference.AssemblyLocation);
                    node.Edges.Add(new Edge() { EdgeType = EdgeType.ProjectReference, TargetNode = targetNode, SourceNode = node });
                });
            }

            return graph;
        }
        private List<Node> CreateClassesGraph(List<AnalyzerResult> analyzerResults)
        {
            var allSourceFileResults = analyzerResults.SelectMany(a => a.ProjectResult.SourceFileResults);

            // All the namespaces in the project. We will use to filter other components
            var allNamespaces = allSourceFileResults.SelectMany(s => s.AllNamespaces())
                .Select(n => n.Identifier)
                .Distinct()
                .ToHashSet();

            // All class declarations. These will represent class nodes
            var allClassDeclarations = allSourceFileResults.SelectMany(s => s.AllClasses());

            var graph = new List<Node>();
            allClassDeclarations.ToList().ForEach(classDeclaration => graph.Add(new Node()
            {
                Name = classDeclaration.Identifier,
                Identifier = classDeclaration.FullIdentifier,
                NodeType = NodeType.Class
            }));

            var interfaceDeclarations = allSourceFileResults.SelectMany(s => s.AllInterfaces());
            interfaceDeclarations.ToList().ForEach(interfaceDeclaration => graph.Add(new Node()
            {
                Name = interfaceDeclaration.Identifier,
                Identifier = interfaceDeclaration.FullIdentifier,
                NodeType = NodeType.Interface
            }));

            foreach (var classDeclaration in allClassDeclarations)
            {
                var currentNode = graph.FirstOrDefault(n => n.Identifier == classDeclaration.FullIdentifier);

                // Inheritance Relations (Interfaces or other classes)
                var baseTypes = classDeclaration.BaseList;
                baseTypes.ForEach(baseType =>
                {
                    var targetNode = graph.FirstOrDefault(t => t.Identifier == baseType);
                    var edge = new Edge()
                    {
                        EdgeType = EdgeType.Inheritance,
                        TargetNode = targetNode,
                        SourceNode = currentNode
                    };
                    currentNode.Edges.Add(edge);
                    targetNode.Edges.Add(edge);
                });

                // Declarations
                var declarations = classDeclaration.AllDeclarationNodes().Where(d => allClassDeclarations.Any(c => c.FullIdentifier == d.FullIdentifier)).ToList();
                declarations.ForEach(declaration =>
                {
                    var targetNode = graph.FirstOrDefault((n) => n.Identifier == declaration.FullIdentifier);
                    if (targetNode.Identifier != currentNode.Identifier)
                    {
                        var edge = new Edge()
                        {
                            EdgeType = EdgeType.Declaration,
                            TargetNode = targetNode,
                            SourceNode = currentNode
                        };
                        currentNode.Edges.Add(edge);
                        targetNode.Edges.Add(edge);
                    }
                });

                // Method Calls
                var invocationExpressions = classDeclaration.AllInvocationExpressions().Where(i => allNamespaces.Contains(i.SemanticNamespace)).ToList();
                invocationExpressions.ForEach(invocation => {
                    var targetNode = graph.FirstOrDefault((n) => n.Identifier == invocation.SemanticFullClassType);

                    //Skip methods in same class
                    if (targetNode.Identifier != currentNode.Identifier)
                    {
                        var edge = new Edge()
                        {
                            EdgeType = invocation is ObjectCreationExpression ? EdgeType.ObjectCreation : EdgeType.Invocation,
                            TargetNode = targetNode,
                            Identifier = invocation.MethodName,
                            SourceNode = currentNode
                        };
                        currentNode.Edges.Add(edge);
                        targetNode.Edges.Add(edge);
                    }
                });

                // Members (Properties)
                var memberExpressions = classDeclaration.AllMemberAccessExpressions().Where(i => allNamespaces.Contains(i.Reference.Namespace)).ToList();
                memberExpressions.ForEach(memberExpression => {
                    var targetNode = graph.FirstOrDefault((n) => n.Identifier == memberExpression.SemanticFullClassType);

                    //Skip methods in same class
                    if (targetNode.Identifier != currentNode.Identifier)
                    {
                        var edge = new Edge()
                        {
                            EdgeType = EdgeType.MemberAccess,
                            TargetNode = targetNode,
                            Identifier = memberExpression.Identifier,
                            SourceNode = currentNode
                        };
                        currentNode.Edges.Add(edge);
                        targetNode.Edges.Add(edge);
                    }
                });

            }

            return graph;
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
        public List<Edge> Edges { get; set; }
        public Dictionary<string, string> Properties { get; set; }
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
    }

    public enum NodeType
    {
        Project,
        Class,
        Interface,
        Method
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
