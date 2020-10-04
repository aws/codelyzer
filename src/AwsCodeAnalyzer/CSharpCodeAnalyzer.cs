using AwsCodeAnalyzer.Build;
using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.CSharp;
using AwsCodeAnalyzer.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
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

            var analyzerResults = new List<AnalyzerResult>();
            var projectBuildResults = new List<ProjectBuildResult>();

            try
            {
                WorkspaceBuilder builder = new WorkspaceBuilder(Log.Logger, path, AnalyzerConfiguration);
                projectBuildResults = await builder.Build();

                List<ProjectWorkspace> workspaceResults = new List<ProjectWorkspace>();

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
            }
            finally
            {
                /* 
                 * Cleanup
                 * https://stackoverflow.com/questions/53360075/csharpscript-memory-leaks-in-asp-net-core-api
                 * https://github.com/dotnet/roslyn/issues/5482
                 */

                if (!AnalyzerConfiguration.MetaDataSettings.LoadBuildData)
                {
                    Cleanup(projectBuildResults);
                }

                Logger.Debug("Memory used before collection:       {0:N0}",
                       GC.GetTotalMemory(false));

                // Collect all generations of memory.
                RunGarbageCollection();
                Logger.Debug("Memory used after full collection:   {0:N0}",
                       GC.GetTotalMemory(true));
            }

            return analyzerResults;
        }

        private void RunGarbageCollection()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (System.Exception)
            {
                //sometimes GC.Collet/WaitForPendingFinalizers crashes
            }
        }

        private void Cleanup(List<ProjectBuildResult> projectBuildResults)
        {
            try
            {
                Logger.Debug("Cleaning up ProjectBuildResults");
                foreach (var projectBuildResult in projectBuildResults)
                {
                    if (projectBuildResult.Compilation != null)
                    {  
                        projectBuildResult.Compilation.RemoveAllReferences();
                        projectBuildResult.Compilation.RemoveAllSyntaxTrees();
                        projectBuildResult.ExternalReferences = null;
                        projectBuildResult.Compilation = null;
                    
                        projectBuildResult.Project = null;

                        foreach ( var sfr in projectBuildResult.SourceFileBuildResults)
                        {
                            sfr.SyntaxTree = null;
                            sfr.SemanticModel = null;
                        }

                        projectBuildResult.SourceFileBuildResults.Clear();

                    }
                }

            }
            catch (Exception e)
            {
                
            }
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