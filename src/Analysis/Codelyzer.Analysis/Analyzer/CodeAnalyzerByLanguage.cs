using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codelyzer.Analysis.Build;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging;

namespace Codelyzer.Analysis.Analyzer
{
    public class CodeAnalyzerByLanguage
    {
        protected readonly AnalyzerConfiguration AnalyzerConfiguration;
        protected readonly ILogger Logger;

        public CodeAnalyzerByLanguage(AnalyzerConfiguration configuration, ILogger logger)
        {
            AnalyzerConfiguration = configuration;
            Logger = logger;
        }
        public async Task<AnalyzerResult> AnalyzeProject(string projectPath)
        {
            AnalyzerResult analyzerResult = (await Analyze(projectPath)).First();
            return analyzerResult;
        }

        public async Task<List<AnalyzerResult>> AnalyzeSolution(string solutionPath)
        {
            return await Analyze(solutionPath);
        }

        public async Task<List<AnalyzerResult>> Analyze(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            List<ProjectWorkspace> workspaceResults = new List<ProjectWorkspace>();
            var analyzerResults = new List<AnalyzerResult>();

            WorkspaceBuilder builder = new WorkspaceBuilder(Logger, path, AnalyzerConfiguration);

            var projectBuildResults = await builder.Build();

            foreach (var projectBuildResult in projectBuildResults)
            {
                var workspaceResult = await Task.Run(() => AnalyzeProject(projectBuildResult));
                workspaceResult.ProjectGuid = projectBuildResult.ProjectGuid;
                workspaceResult.ProjectType = projectBuildResult.ProjectType;
                workspaceResults.Add(workspaceResult);

                //Generate Output result
                if (AnalyzerConfiguration.MetaDataSettings.LoadBuildData)
                {
                    analyzerResults.Add(new AnalyzerResult() { ProjectResult = workspaceResult, ProjectBuildResult = projectBuildResult });
                }
                else
                {
                    analyzerResults.Add(new AnalyzerResult() { ProjectResult = workspaceResult });
                }
            }

            await GenerateOptionalOutput(analyzerResults);

            return analyzerResults;
        }

        private async Task GenerateOptionalOutput(List<AnalyzerResult> analyzerResults)
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
        public ProjectWorkspace AnalyzeProject(ProjectBuildResult projectResult)
        {
            Logger.LogDebug("Analyzing the project: " + projectResult.ProjectPath);
            var projType = Path.GetExtension(projectResult.ProjectPath).ToLower();
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

            foreach (var fileBuildResult in projectResult.SourceFileBuildResults)
            {
                var fileAnalysis = languageAnalyzer.AnalyzeFile(fileBuildResult, workspace.ProjectRootPath);
                workspace.SourceFileResults.Add(fileAnalysis);
            }

            return workspace;
        }

        public LanguageAnalyzer GetLanguageAnalyzerByProjectType(string projType)
        {
            LanguageAnalyzerFactory languageAnalyzerFactory;
            switch (projType.ToLower())
            {
                case ".vbproj":
                    languageAnalyzerFactory = new VBAnalyerFactory(AnalyzerConfiguration, Logger);
                    break;
                case ".csproj":
                    languageAnalyzerFactory = new CSharpAnalyzerFactory(AnalyzerConfiguration, Logger);
                    break;

                default:
                    throw new Exception($"invalid project type {projType}");
            }
            return languageAnalyzerFactory.GetLanguageAnalyzer();
            
        }

        public LanguageAnalyzer GetLanguageAnalyzerByFileType(string fileType)
        {
            LanguageAnalyzerFactory languageAnalyzerFactory;
            switch (fileType.ToLower())
            {
                case ".vb":
                    languageAnalyzerFactory = new VBAnalyerFactory(AnalyzerConfiguration, Logger);
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
