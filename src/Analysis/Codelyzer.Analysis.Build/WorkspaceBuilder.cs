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

        public async Task<List<ProjectBuildResult>> Build()
        {
            using(var builder = new WorkspaceBuilderHelper(Logger, _workspacePath, _analyzerConfiguration))
            {
                builder.Build();
                foreach(var projectResult in builder.Projects)
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

    }
}
