using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Codelyzer.Analysis
{
    public class CompactGraph : AbstractGraph
    {
        public CompactGraph(ILogger logger) : base(logger)
        {
            Graph = new ConcurrentDictionary<Node, string>();
            ConcurrentEdges = new ConcurrentBag<Edge>();
        }
        public HashSet<Node> Nodes { get; set; }
        public List<Edge> Edges { get; set; }
        [JsonIgnore]
        public ConcurrentBag<Edge> ConcurrentEdges { get; set; }

        public void CreateSerializedForm()
        {
            Nodes = Graph.Keys.ToHashSet();
            Edges = ConcurrentEdges.ToList();
            Graph = null;
            ConcurrentEdges = null;
        }

        public void CreateDeserializedForm()
        {
            Graph = new ConcurrentDictionary<Node, string>(Nodes.ToDictionary(n => n, n => n.Identifier));
            ConcurrentEdges = new ConcurrentBag<Edge>(Edges);
            Nodes = null;
            Edges = null;
        }

        public void Merge(ConcurrentBag<CompactGraph> compactGraphs)
        {
            Parallel.ForEach(compactGraphs, compactGraph =>
            {
                foreach(var edge in compactGraph.ConcurrentEdges)
                {
                    ConcurrentEdges.Add(edge);
                }
                foreach (var node in compactGraph.Graph)
                {
                    Graph.TryAdd(node.Key, node.Value);
                }
            });
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
                    projectReferences?.ForEach(projectReference =>
                    {
                        ConcurrentEdges.Add(new Edge()
                        {
                            SourceNodeId = projectResult.Identifier,
                            TargetNodeId = projectReference,
                            EdgeType = EdgeType.ProjectReference
                        });
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
                ConcurrentEdges.Add(new Edge() { SourceNodeId = sourceNode.Identifier, TargetNodeId = baseType, EdgeType = EdgeType.Inheritance });
            });
        }
        protected override void CreateEdges(Node sourceNode)
        {
            var edgeCandidates = filteredUstNodeEdgeCandidates[sourceNode].ToList();

            Parallel.ForEach(edgeCandidates, edgeCandidate =>
            {
                var newEdge = new Edge() { SourceNodeId = sourceNode.Identifier, TargetNodeId = edgeCandidate.FullIdentifier };
                ConcurrentEdges.Add(newEdge);
                if (edgeCandidate is DeclarationNode)
                {
                    newEdge.EdgeType = EdgeType.Declaration;
                }
                else if (edgeCandidate is MemberAccess memberAccess)
                {
                    newEdge.TargetNodeId = memberAccess.SemanticFullClassTypeName;
                    newEdge.EdgeType = EdgeType.MemberAccess;
                }
                else if (edgeCandidate is InvocationExpression invocation)
                {
                    if (invocation is ObjectCreationExpression objectCreationExpression)
                    {
                        newEdge.EdgeType = EdgeType.ObjectCreation;
                        ConcurrentEdges.Add(new Edge() { SourceNodeId = sourceNode.Identifier, TargetNodeId = objectCreationExpression.SemanticFullClassTypeName, EdgeType = EdgeType.ObjectCreation });
                    }
                    else
                    {
                        newEdge.EdgeType = EdgeType.Invocation;
                    }
                }
            });
        }
    }
}
