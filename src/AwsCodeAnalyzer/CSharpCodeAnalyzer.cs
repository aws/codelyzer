using AwsCodeAnalyzer.Build;
using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.CSharp;
using AwsCodeAnalyzer.Model;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AwsCodeAnalyzer
{
    /// <summary>
    /// Code analyzer for CSharp
    /// </summary>
    public class CSharpCodeAnalyzer : CodeAnalyzer
    {
        public CSharpCodeAnalyzer(AnalyzerConfiguration configuration, ILogger logger)
            : base(configuration, logger)
        {
        }

        public override async Task<AnalyzerResult> AnalyzeProject(string projectPath)
        {
            AnalyzerResult analyzerResult = (await Analyze(projectPath)).First();
            return analyzerResult;
        }

        public override async Task<List<AnalyzerResult>> AnalyzeSolution(string solutionPath)
        {
            return await Analyze(solutionPath);
        }

        private async Task<List<AnalyzerResult>> Analyze(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }
            
            WorkspaceBuilder builder = new WorkspaceBuilder(Log.Logger, path, AnalyzerConfiguration);
            var projectBuildResults = await builder.Build();

            List<ProjectWorkspace> workspaceResults = new List<ProjectWorkspace>();
            var analyzerResults = new List<AnalyzerResult>();
            foreach (var projectBuildResult in projectBuildResults)
            {
                var workspaceResult = await AnalyzeProject(projectBuildResult);
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
                FileUtils.CreateDirectory(AnalyzerConfiguration.ExportSettings.OutputPath);
                foreach (var analyzerResult in analyzerResults)
                {
                    Logger.Debug("Generating Json file for " + analyzerResult.ProjectResult.ProjectName);
                    var jsonOutput = SerializeUtils.ToJson<ProjectWorkspace>(analyzerResult.ProjectResult);
                    var jsonFilePath = await FileUtils.WriteFileAsync(AnalyzerConfiguration.ExportSettings.OutputPath, 
                        analyzerResult.ProjectResult.ProjectName+".json", jsonOutput);
                    analyzerResult.OutputJsonFilePath = jsonFilePath;
                    Logger.Debug("Generated Json file  " + jsonFilePath);
                }
            }
        }

        private async Task<ProjectWorkspace> AnalyzeProject(ProjectBuildResult projectResult)
        {
            Logger.Debug("Analyzing the project: " + projectResult.ProjectPath);
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
                CodeContext codeContext = new CodeContext(fileBuildResult.SemanticModel,
                    fileBuildResult.SyntaxTree,
                    workspace.ProjectRootPath,
                    fileBuildResult.SourceFilePath,
                    AnalyzerConfiguration,
                    Log.Logger);

                Log.Logger.Debug("Analyzing: " + fileBuildResult.SourceFileFullPath);
                
                CSharpRoslynProcessor processor = new CSharpRoslynProcessor(codeContext);
                var result = processor.Visit(codeContext.SyntaxTree.GetRoot());
                workspace.SourceFileResults.Add((RootUstNode)result);
            }
            
            return workspace;
        }
    }
}