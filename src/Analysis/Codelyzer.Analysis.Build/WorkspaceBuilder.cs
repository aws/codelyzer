using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
                var projectResultEnumerator = builder.BuildProjectIncremental().GetAsyncEnumerator();

                try
                {
                    while (await projectResultEnumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        var result = projectResultEnumerator.Current;

                        if (result.AnalyzerResult != null)
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
                }
                finally
                {
                    await projectResultEnumerator.DisposeAsync();
                }
            }
        }

        public async Task<List<ProjectBuildResult>> Build()
        {
            using (var builder = new WorkspaceBuilderHelper(Logger, _workspacePath, _analyzerConfiguration))
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

            return ProjectResults;
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
