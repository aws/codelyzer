using AwsCodeAnalyzer.Common;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AwsCodeAnalyzer.Build
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
            using(var builder = new WorkspaceBuilderHelper(Logger, _workspacePath))
            {
                builder.Build();
                foreach(var projectResult in builder.Projects)
                {
                    using (ProjectBuildHandler projectBuildHandler = new ProjectBuildHandler(Logger, projectResult.Project, _analyzerConfiguration))
                    {
                        projectBuildHandler.AnalyzerResult = projectResult.AnalyzerResult;
                        projectBuildHandler.ProjectAnalyzer = projectResult.ProjectAnalyzer;
                        var result = await projectBuildHandler.Build();
                        ProjectResults.Add(result);
                    }
                }
            }

            return ProjectResults;
        }

    }
}
