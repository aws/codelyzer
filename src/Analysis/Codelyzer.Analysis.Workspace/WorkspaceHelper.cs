using System;
using System.Collections.Generic;
using Codelyzer.Analysis.Model.Build;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codelyzer.Analysis.Common;
using Microsoft.Build.Construction;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging;

namespace Codelyzer.Analysis.Workspace
{
    public class WorkspaceHelper : IWorkspaceHelper
    {
        private ILogger _logger;
        private ProjectBuildHelper _projectBuildHelper;

        public WorkspaceHelper(ILogger logger)
        {
            _logger = logger;
            _projectBuildHelper = new ProjectBuildHelper(_logger);
        }
        
        public async Task<List<ProjectBuildResult>> GetProjectBuildResults(Solution solution)
        {
            var buildResults = new List<ProjectBuildResult>();

            var projectMap = _projectBuildHelper.GetProjectInSolutionObjects(solution.FilePath);

            foreach (var project in solution.Projects)
            {
                buildResults.Add(await GetProjectBuildResult(project, projectMap));
            }

            return buildResults;
        }

        public async IAsyncEnumerable<ProjectBuildResult> GetProjectBuildResultsGeneratorAsync(Solution solution)
        {
            var projectMap = _projectBuildHelper
                .GetProjectInSolutionObjects(solution.FilePath);

            foreach (var project in solution.Projects)
            {
                yield return await GetProjectBuildResult(project, projectMap);
            }
        }

        private async Task<ProjectBuildResult> GetProjectBuildResult(Project project, Dictionary<string, ProjectInSolution> projectMap)
        {
            var compilation = await project.GetCompilationAsync() ?? throw new Exception("Get compilation failed");

            // await SetCompilation(); maybe we should refactor the fallback compilation? but with vs workspace, we shouldn't need this faillback

            var (prePortCompilation, prePortMetaReferences, missingMetaReferences) =
                await _projectBuildHelper.GetPrePortCompilation(project);
            var projectBuildResult = new ProjectBuildResult
            {
                BuildErrors = compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(error => error.ToString())
                .ToList(),
                ProjectPath = project.FilePath,
                ProjectRootPath = Path.GetDirectoryName(project.FilePath),
                Project = project,
                Compilation = compilation,
                PrePortCompilation = prePortCompilation,
                IsSyntaxAnalysis = false, // I don't think we should ever hit this.
                PreportReferences = prePortMetaReferences,
                MissingReferences = missingMetaReferences,

                //GetTargetFrameworks(projectBuildResult, AnalyzerResult); todo: get this? 

                ProjectGuid = (
                projectMap.TryGetValue(project.Name, out var projectInSolution)
                    ? Guid.Parse(projectInSolution.ProjectGuid)
                    : Guid.NewGuid())
                .ToString(),
                ProjectType = projectInSolution != null ? projectInSolution.ProjectType.ToString() : string.Empty
            };

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var sourceFilePath = PathNetCore.GetRelativePath(projectBuildResult.ProjectRootPath, syntaxTree.FilePath);
                var prePortTree = prePortCompilation?.SyntaxTrees?.FirstOrDefault(s => s.FilePath == syntaxTree.FilePath);
                var fileResult = new SourceFileBuildResult
                {
                    SyntaxTree = syntaxTree,
                    PrePortSemanticModel = prePortTree != null ? prePortCompilation?.GetSemanticModel(prePortTree) : null,
                    SemanticModel = compilation.GetSemanticModel(syntaxTree),
                    SourceFileFullPath = syntaxTree.FilePath,
                    SyntaxGenerator = SyntaxGenerator.GetGenerator(project),
                    SourceFilePath = sourceFilePath
                };
                projectBuildResult.SourceFileBuildResults.Add(fileResult);
                projectBuildResult.SourceFiles.Add(sourceFilePath);
            }

            projectBuildResult.ExternalReferences = _projectBuildHelper.GetExternalReferences(
                projectBuildResult?.Compilation,
                projectBuildResult?.Project);

            return projectBuildResult ?? new ProjectBuildResult();
        }
    }
}
