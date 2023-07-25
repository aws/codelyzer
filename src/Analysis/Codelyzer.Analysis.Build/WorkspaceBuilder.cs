using Codelyzer.Analysis.Common;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codelyzer.Analysis.Model.Build;

namespace Codelyzer.Analysis.Build
{
    public class WorkspaceBuilder
    {
        private readonly ILogger Logger;
        private readonly AnalyzerConfiguration _analyzerConfiguration;
        private readonly string _workspacePath;

        private List<ProjectBuildResult> ProjectResults { get; }


        public WorkspaceBuilder(ILogger logger, string workspacePath, AnalyzerConfiguration analyzerConfiguration = null)
        {
            this.ProjectResults = new List<ProjectBuildResult>();
            this._workspacePath = workspacePath;
            this.Logger = logger;
            _analyzerConfiguration = analyzerConfiguration;
        }
       
        public async IAsyncEnumerable<ProjectBuildResult> BuildProject()
        {         
            using (var builder = new WorkspaceBuilderHelper(Logger, _workspacePath, _analyzerConfiguration))
            {
                try
                {
                    if (!_analyzerConfiguration.BuildSettings.SyntaxOnly)
                    {
                        var projectResultEnumerator = builder.BuildProjectIncremental().GetAsyncEnumerator();
                        while (await projectResultEnumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            var result = projectResultEnumerator.Current;
                            if (result?.AnalyzerResult != null)
                            {
                                using (ProjectBuildHandler projectBuildHandler = new ProjectBuildHandler(Logger, result.Project, _analyzerConfiguration))
                                {
                                    projectBuildHandler.AnalyzerResult = result.AnalyzerResult;
                                    projectBuildHandler.ProjectAnalyzer = result.ProjectAnalyzer;
                                    var projectBuildResult = await projectBuildHandler.Build();
                                    yield return projectBuildResult;
                                }
                            }
                            else
                            {
                                if (_analyzerConfiguration.AnalyzeFailedProjects)
                                {
                                    using (ProjectBuildHandler projectBuildHandler = new ProjectBuildHandler(Logger, _analyzerConfiguration))
                                    {
                                        projectBuildHandler.ProjectAnalyzer = result.ProjectAnalyzer;
                                        var projectBuildResult = projectBuildHandler.SyntaxOnlyBuild();
                                        yield return projectBuildResult;
                                    }
                                }
                            }
                        }
                        await projectResultEnumerator.DisposeAsync();
                    }
                    else 
                    {
                        var projectReferencesMap = FileUtils.GetProjectsWithReferences(_workspacePath);
                        builder.GenerateNoBuildAnalysis();

                        var projectsInOrder = CreateDependencyQueue(projectReferencesMap);
                        Dictionary<string, MetadataReference> references = new Dictionary<string, MetadataReference>();

                        foreach (string projectPath in projectsInOrder)
                        {
                            var project = builder.Projects.Find(p => p.ProjectAnalyzer.ProjectFile.Path.Equals(projectPath, StringComparison.InvariantCultureIgnoreCase));
                            var projectReferencePaths = projectReferencesMap[projectPath]?.Distinct().ToHashSet<string>();

                            using (ProjectBuildHandler projectBuildHandler =
                                   new ProjectBuildHandler(Logger, project.Project, _analyzerConfiguration))
                            {
                                projectBuildHandler.AnalyzerResult = project.AnalyzerResult;
                                projectBuildHandler.ProjectAnalyzer = project.ProjectAnalyzer;
                                var projectReferences = references.Where(r => projectReferencePaths.Contains(r.Key)).ToDictionary(p => p.Key, p => p.Value);
                                var result = projectBuildHandler.SyntaxOnlyBuild(projectReferences);
                                if (result != null)
                                {
                                    references.Add(projectPath, result.Compilation.ToMetadataReference());
                                    yield return result;
                                }
                            }
                        }
                    }
                }
                finally
                {
                }
            }
        }

        public async Task<List<ProjectBuildResult>> Build()
        {
            using (var builder = new WorkspaceBuilderHelper(Logger, _workspacePath, _analyzerConfiguration))
            {
                if (!_analyzerConfiguration.BuildSettings.SyntaxOnly)
                {
                    builder.Build();
                    foreach (var projectResult in builder.Projects)
                    {
                        using (ProjectBuildHandler projectBuildHandler =
                            new ProjectBuildHandler(Logger, projectResult.Project, _analyzerConfiguration))
                        {
                            projectBuildHandler.AnalyzerResult = projectResult.AnalyzerResult;
                            projectBuildHandler.ProjectAnalyzer = projectResult.ProjectAnalyzer;
                            var result = await projectBuildHandler.Build();
                            ProjectResults.Add(result);
                        }
                    }
                    if (_analyzerConfiguration.AnalyzeFailedProjects)
                    {
                        foreach (var projectResult in builder.FailedProjects)
                        {
                            using (ProjectBuildHandler projectBuildHandler =
                            new ProjectBuildHandler(Logger, _analyzerConfiguration))
                            {
                                projectBuildHandler.ProjectAnalyzer = projectResult.ProjectAnalyzer;
                                var result = projectBuildHandler.SyntaxOnlyBuild();
                                ProjectResults.Add(result);
                            }
                        }
                    }
                } 
                else
                {
                    var projectReferencesMap = FileUtils.GetProjectsWithReferences(_workspacePath);
                    builder.GenerateNoBuildAnalysis();

                    var projectsInOrder = CreateDependencyQueue(projectReferencesMap);
                    Dictionary<string, MetadataReference> references = new Dictionary<string, MetadataReference>();

                    while (projectsInOrder.Count > 0)
                    {
                        var projectPath = projectsInOrder.Dequeue();
                        var project = builder.Projects.Find(p => p.ProjectAnalyzer.ProjectFile.Path.Equals(projectPath, StringComparison.InvariantCultureIgnoreCase));
                        var projectReferencePaths = projectReferencesMap[projectPath]?.Distinct().ToHashSet<string>();

                        using (ProjectBuildHandler projectBuildHandler =
                            new ProjectBuildHandler(Logger, project.Project, _analyzerConfiguration))
                        {
                            projectBuildHandler.AnalyzerResult = project.AnalyzerResult;
                            projectBuildHandler.ProjectAnalyzer = project.ProjectAnalyzer;
                            var projectReferences = references.Where(r => projectReferencePaths.Contains(r.Key)).ToDictionary(p=>p.Key, p=> p.Value);
                            var result = projectBuildHandler.SyntaxOnlyBuild(projectReferences);
                            if (result != null)
                            {
                                references.Add(projectPath, result.Compilation.ToMetadataReference());
                                ProjectResults.Add(result);
                            }
                        }
                    }
                }
            }

            return ProjectResults;
        }

        private Queue<string> CreateDependencyQueue(Dictionary<string, HashSet<string>> projectReferencesMap)
        {
            var projectsInOrder = new Queue<string>();
            var builtProjects = new HashSet<string>();
            foreach (var project in projectReferencesMap.Keys)
            {
                try
                {
                    if (!builtProjects.Contains(project))
                    {
                        CreateDependencyQueueHelper(project, builtProjects, projectReferencesMap, projectsInOrder);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error while getting dependencies for project {project}");
                }
            }
            return projectsInOrder;
        }

        private void CreateDependencyQueueHelper(string projectPath, HashSet<string> builtProjects, Dictionary<string, HashSet<string>> projectReferencesMap, Queue<string> buildOrder)
        {
            builtProjects.Add(projectPath);

            foreach (var dependency in projectReferencesMap[projectPath])
            {
                if (!builtProjects.Contains(dependency))
                    CreateDependencyQueueHelper(dependency, builtProjects, projectReferencesMap, buildOrder);
            }

            buildOrder.Enqueue(projectPath);
        }

        public List<ProjectBuildResult> GenerateNoBuildAnalysis(Dictionary<string, List<string>> oldReferences, Dictionary<string, List<string>> references)
        {
            using (var builder = new WorkspaceBuilderHelper(Logger, _workspacePath, _analyzerConfiguration))
            {
                builder.GenerateNoBuildAnalysis();

                foreach (var projectResult in builder.Projects)
                {
                    var projectPath = projectResult.ProjectAnalyzer.ProjectFile.Path;
                    var oldRefs = oldReferences?.ContainsKey(projectPath) == true ? oldReferences[projectPath] : null;
                    var refs = references?.ContainsKey(projectPath) == true ? references[projectPath] : null;

                    using (ProjectBuildHandler projectBuildHandler =
                    new ProjectBuildHandler(Logger, projectPath, oldRefs, refs, _analyzerConfiguration))
                    {
                        projectBuildHandler.ProjectAnalyzer = projectResult.ProjectAnalyzer;
                        var result = projectBuildHandler.ReferenceOnlyBuild();
                        ProjectResults.Add(result);
                    }
                }
            }
            return ProjectResults;
        }
    }
}
