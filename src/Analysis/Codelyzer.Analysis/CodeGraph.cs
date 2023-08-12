using Codelyzer.Analysis.Model;
using Microsoft.Build.Logging.StructuredLogger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NuGet.Packaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Codelyzer.Analysis
{
    public class CodeGraph : AbstractGraph
    {
        public CodeGraph(ILogger logger) : base(logger)
        {
            Logger = logger;
            Graph = new ConcurrentDictionary<Node, string>();
        }
        public override void Initialize(List<AnalyzerResult> analyzerResults)
        {
            PopulateGraphs(analyzerResults);
        }
        protected override void AddProjectEdges()
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
        protected override void CreateClassHierarchyEdges(Node sourceNode, List<string> baseTypes, string baseTypeOriginalDefinition)
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
        protected override void CreateEdges(Node sourceNode)
        {
            var edgeCandidates = filteredUstNodeEdgeCandidates[sourceNode].ToList();
            Parallel.ForEach(edgeCandidates, edgeCandidate =>
            {
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
    }
}
