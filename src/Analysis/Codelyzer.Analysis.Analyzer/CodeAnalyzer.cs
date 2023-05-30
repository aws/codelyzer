using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Codelyzer.Analysis.Model.Build;
using Codelyzer.Analysis.Workspace;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Codelyzer.Analysis.Analyzers
{
    public class CodeAnalyzer
    {
        protected readonly AnalyzerConfiguration AnalyzerConfiguration;
        protected readonly ILogger Logger;

        public CodeAnalyzer(AnalyzerConfiguration configuration, ILogger logger)
        {
            AnalyzerConfiguration = configuration;
            Logger = logger;
        }

        public async Task<List<AnalyzerResult>> Analyze(Solution solution)
        {
            var projectBuildResults = await new WorkspaceHelper(Logger).GetProjectBuildResults(solution);
            return await Analyze(projectBuildResults);
        }

        public async Task<List<AnalyzerResult>> Analyze(IEnumerable<ProjectBuildResult> projectBuildResults)
        {
            var analyzerResults = new List<AnalyzerResult>();

            foreach (var projectBuildResult in projectBuildResults)
            {
                analyzerResults.Add(await AnalyzeProjectBuildResult(projectBuildResult));
            }
            await GenerateOptionalOutput(analyzerResults);
            return analyzerResults;
        }

        public async Task<AnalyzerResult> AnalyzeProjectBuildResult(ProjectBuildResult projectBuildResult)
        {
            var workspaceResult = await Task.Run(() => AnalyzeProject(projectBuildResult));
            workspaceResult.ProjectGuid = projectBuildResult.ProjectGuid;
            workspaceResult.ProjectType = projectBuildResult.ProjectType;

            //Generate Output result
            return AnalyzerConfiguration.MetaDataSettings.LoadBuildData
                ? new AnalyzerResult()
                {
                    ProjectResult = workspaceResult,
                    ProjectBuildResult = projectBuildResult
                }
                : new AnalyzerResult()
                {
                    ProjectResult = workspaceResult
                };
        }

        public async Task<AnalyzerResult> AnalyzeFile(
            string filePath,
            ProjectBuildResult incrementalBuildResult,
            AnalyzerResult analyzerResult)
        {
            var newSourceFileBuildResult =
                incrementalBuildResult.SourceFileBuildResults.FirstOrDefault(sourceFile =>
                    sourceFile.SourceFileFullPath == filePath);
            var languageAnalyzer = GetLanguageAnalyzerByFileType(Path.GetExtension(filePath));
            var fileAnalysis = languageAnalyzer.AnalyzeFile(newSourceFileBuildResult,
                analyzerResult.ProjectResult.ProjectRootPath);
            analyzerResult.ProjectResult.SourceFileResults.Add(fileAnalysis);
            return analyzerResult;
        }

        public async Task GenerateOptionalOutput(List<AnalyzerResult> analyzerResults)
        {
            if (AnalyzerConfiguration.ExportSettings.GenerateJsonOutput)
            {
                Directory.CreateDirectory(AnalyzerConfiguration.ExportSettings.OutputPath);
                foreach (var analyzerResult in analyzerResults)
                {
                    Logger.LogDebug("Generating Json file for " + analyzerResult.ProjectResult.ProjectName);
                    var jsonOutput = SerializeUtils.ToJson<ProjectWorkspace>(analyzerResult.ProjectResult);
                    var jsonFilePath = await FileUtils.WriteFileAsync(AnalyzerConfiguration.ExportSettings.OutputPath,
                        analyzerResult.ProjectResult.ProjectName + ".json", jsonOutput);
                    analyzerResult.OutputJsonFilePath = jsonFilePath;
                    Logger.LogDebug("Generated Json file  " + jsonFilePath);
                }
            }
        }

        public LanguageAnalyzer GetLanguageAnalyzerByProjectType(string projType)
        {
            LanguageAnalyzerFactory languageAnalyzerFactory;
            switch (projType.ToLower())
            {
                case ".vbproj":
                    languageAnalyzerFactory = new VbAnalyzerFactory(AnalyzerConfiguration, Logger);
                    break;
                case ".csproj":
                    languageAnalyzerFactory = new CSharpAnalyzerFactory(AnalyzerConfiguration, Logger);
                    break;

                default:
                    throw new Exception($"invalid project type {projType}");
            }
            return languageAnalyzerFactory.GetLanguageAnalyzer();
        }

        private ProjectWorkspace AnalyzeProject(ProjectBuildResult projectResult)
        {
            // Logger.LogDebug("Analyzing the project: " + projectResult.ProjectPath);
            var projType = Path.GetExtension(projectResult.ProjectPath)?.ToLower();
            LanguageAnalyzer languageAnalyzer = GetLanguageAnalyzerByProjectType(projType);
            ProjectWorkspace workspace = new ProjectWorkspace(projectResult.ProjectPath)
            {
                SourceFiles = new UstList<string>(projectResult.SourceFiles),
                BuildErrors = projectResult.BuildErrors,
                BuildErrorsCount = projectResult.BuildErrors.Count
            };

            if (AnalyzerConfiguration.MetaDataSettings.ReferenceData)
            {
                workspace.ExternalReferences = projectResult.ExternalReferences;
            }
            workspace.TargetFramework = projectResult.TargetFramework;
            workspace.TargetFrameworks = projectResult.TargetFrameworks;
            workspace.LinesOfCode = 0;
            foreach (var fileBuildResult in projectResult.SourceFileBuildResults)
            {
                var fileAnalysis = languageAnalyzer.AnalyzeFile(fileBuildResult, workspace.ProjectRootPath);
                workspace.LinesOfCode += fileAnalysis.LinesOfCode;
                workspace.SourceFileResults.Add(fileAnalysis);
            }

            return workspace;
        }

        public LanguageAnalyzer GetLanguageAnalyzerByFileType(string fileType)
        {
            LanguageAnalyzerFactory languageAnalyzerFactory;
            switch (fileType.ToLower())
            {
                case ".vb":
                    languageAnalyzerFactory = new VbAnalyzerFactory(AnalyzerConfiguration, Logger);
                    break;
                case ".cs":
                    languageAnalyzerFactory = new CSharpAnalyzerFactory(AnalyzerConfiguration, Logger);
                    break;

                default:
                    throw new Exception($"invalid project type {fileType}");
            }
            return languageAnalyzerFactory.GetLanguageAnalyzer();

        }
    }
}
