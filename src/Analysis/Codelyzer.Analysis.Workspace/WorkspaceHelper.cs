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

namespace Codelyzer.Analysis.VsWorkspace
{
    public class WorkspaceHelper
    {
        public async Task<List<ProjectBuildResult>> GetProjectBuildResults(Solution solution)
        {
            var buildResults = new List<ProjectBuildResult>();

            var projectMap = new ProjectBuildHelper().GetProjectInSolutionObjects(solution.FilePath);

            foreach (var project in solution.Projects)
            {
                buildResults.Add(await GetProjectBuildResult(project, projectMap));
            }

            return buildResults;
        }

        public async IAsyncEnumerable<ProjectBuildResult> GetProjectBuildResultsGeneratorAsync(Solution solution)
        {
            var projectMap = new ProjectBuildHelper().GetProjectInSolutionObjects(solution.FilePath);

            foreach (var project in solution.Projects)
            {
                yield return await GetProjectBuildResult(project, projectMap);
            }
        }

        public async Task<ProjectBuildResult> GetProjectBuildResult(Project project, Dictionary<string, ProjectInSolution> projectMap)
        {
            var compilation = await project.GetCompilationAsync() ?? throw new Exception("Get compilation failed");

            // await SetCompilation(); maybe we should refactor the fallback compilation? but with vs workspace, we shouldn't need this faillback

            var (prePortCompilation, prePortMetaReferences, missingMetaReferences) = await GetPrePortCompilation(project);
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

            projectBuildResult.ExternalReferences = new ProjectBuildHelper().GetExternalReferences(
                projectBuildResult?.Compilation,
                projectBuildResult?.Project);

            return projectBuildResult ?? new ProjectBuildResult();
        }

        private static async Task<(Compilation?, List<string?>, List<string>)> GetPrePortCompilation(Project project )
        {
            var projectBuildHelper = new ProjectBuildHelper();
            var projectFile = projectBuildHelper.LoadProjectFile(project.FilePath);
            if (projectFile == null)
            {
                return (null, new List<string?>(), new List<string>());
            }
            var prePortReferences = projectBuildHelper.LoadMetadataReferences(
                projectFile,
                out var missingMetaReferences);
            if (prePortReferences.Count > 0)
            {
                var prePortProject = project.WithMetadataReferences(prePortReferences);
                var prePortMetaReferences = prePortReferences.Select(m => m.Display).ToList();
                var prePortCompilation = await prePortProject.GetCompilationAsync();
                return (prePortCompilation, prePortMetaReferences, missingMetaReferences);
            }
            return (null, new List<string?>(), new List<string>());
        }
    }
}
