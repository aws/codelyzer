using Codelyzer.Analysis.Model.Build;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codelyzer.Analysis.Workspace
{
    public interface IWorkspaceHelper
    {
        public Task<List<ProjectBuildResult>> GetProjectBuildResults(Solution solution);

        public IAsyncEnumerable<ProjectBuildResult> GetProjectBuildResultsGeneratorAsync(Solution solution);
    }
}
