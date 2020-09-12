using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace AwsCodeAnalyzer.Build
{
    public class WorkspaceBuilder
    {
        private readonly WorkspaceBuilderHelper _builder;
        private readonly ILogger Logger;
        private List<ProjectBuildResult> ProjectResults { get; }

        public WorkspaceBuilder(ILogger logger, string workspacePath)
        {
            this.ProjectResults = new List<ProjectBuildResult>();
            this._builder = new WorkspaceBuilderHelper(logger, workspacePath);
            this.Logger = logger;
        }

        public async Task<List<ProjectBuildResult>> Build()
        {
            this._builder.Build();
            foreach (var project in _builder.Projects)
            {
                ProjectBuildHandler projectBuildHandler = new ProjectBuildHandler(Logger, project);
                var result = await projectBuildHandler.Build();
                result.TargetFramework = _builder.ProjectFrameworkVersions[project.Id.Id.ToString()];
                ProjectResults.Add(result);
            }

            return ProjectResults;
        }
    }
}
