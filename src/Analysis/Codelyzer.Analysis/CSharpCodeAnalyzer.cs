using Codelyzer.Analysis.Build;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.CSharp;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Codelyzer.Analysis
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

        /// <summary>
        /// Analyze a C# project file
        /// </summary>
        /// <param name="projectPath">The path to the project file</param>
        /// <returns></returns>
        public override async Task<AnalyzerResult> AnalyzeProject(string projectPath)
        {
            AnalyzerResult analyzerResult = (await Analyze(projectPath)).First();
            return analyzerResult;
        }

        /// <summary>
        /// Analyzes a solution with C# projects
        /// </summary>
        /// <param name="solutionPath">The path to the solution file</param>
        /// <returns></returns>
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
                FileUtils.CreateDirectory(AnalyzerConfiguration.ExportSettings.OutputPath);
                foreach (var analyzerResult in analyzerResults)
                {
                    Logger.LogDebug("Generating Json file for " + analyzerResult.ProjectResult.ProjectName);
                    var jsonOutput = SerializeUtils.ToJson<ProjectWorkspace>(analyzerResult.ProjectResult);
                    var jsonFilePath = await FileUtils.WriteFileAsync(AnalyzerConfiguration.ExportSettings.OutputPath, 
                        analyzerResult.ProjectResult.ProjectName+".json", jsonOutput);
                    analyzerResult.OutputJsonFilePath = jsonFilePath;
                    Logger.LogDebug("Generated Json file  " + jsonFilePath);
                }
            }
        }

        private ProjectWorkspace AnalyzeProject(ProjectBuildResult projectResult)
        {
            Logger.LogDebug("Analyzing the project: " + projectResult.ProjectPath);
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
                    Logger);

                Logger.LogDebug("Analyzing: " + fileBuildResult.SourceFileFullPath);

                using (CSharpRoslynProcessor processor = new CSharpRoslynProcessor(codeContext))
                {
                    var result = processor.Visit(codeContext.SyntaxTree.GetRoot());
                    workspace.SourceFileResults.Add((RootUstNode)result);
                }
            }
            
            return workspace;
        }
    }
}
