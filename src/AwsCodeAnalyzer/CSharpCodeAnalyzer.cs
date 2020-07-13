using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwsCodeAnalyzer.Build;
using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.CSharp;
using AwsCodeAnalyzer.Model;
using Serilog;

namespace AwsCodeAnalyzer
{
    public class CSharpCodeAnalyzer : CodeAnalyzer
    {
        public CSharpCodeAnalyzer(AnalyzerOptions options, ILogger logger)
            : base(options, logger)
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
            
            WorkspaceBuilder builder = new WorkspaceBuilder(Log.Logger, path);
            var projectBuildResults = await builder.Build();

            List<ProjectWorkspace> workspaceResults = new List<ProjectWorkspace>();

            foreach (var projectBuildResult in projectBuildResults)
            {
                var workspaceResult =  await AnalyzeProject(projectBuildResult);
                workspaceResults.Add(workspaceResult);
            }

            List<AnalyzerResult> analyzerResults = new List<AnalyzerResult>();
            FileUtils.CreateDirectory(AnalyzerOptions.JsonOutputPath);
            
            foreach (var workspaceResult in workspaceResults)
            {
                Logger.Debug("Generating Json file for " + workspaceResult.ProjectName);
                var jsonOutput = SerializeUtils.ToJson<ProjectWorkspace>(workspaceResult);
                var jsonFilePath = await FileUtils.WriteFileAsync(AnalyzerOptions.JsonOutputPath, 
                    workspaceResult.ProjectName+".json", jsonOutput);
                
                Logger.Debug("Generated Json file  " + jsonFilePath);
                
                AnalyzerResult result = new AnalyzerResult
                {
                    ProjectResult = workspaceResult,
                    OutputJsonFilePath = jsonFilePath
                };
                analyzerResults.Add(result);
            }

            return analyzerResults;
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

            foreach (var fileBuildResult in projectResult.SourceFileBuildResults)
            {
                CodeContext codeContext = new CodeContext(fileBuildResult.SemanticModel,
                    fileBuildResult.SyntaxTree,
                    workspace.ProjectRootPath,
                    fileBuildResult.SourceFilePath,
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