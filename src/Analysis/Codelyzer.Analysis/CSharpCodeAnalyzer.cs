using Codelyzer.Analysis.Build;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.CSharp;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
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

        ///<inheritdoc/>
        public override async Task<AnalyzerResult> AnalyzeProject(string projectPath)
        {
            AnalyzerResult analyzerResult = (await Analyze(projectPath)).First();
            return analyzerResult;
        }
        public override async Task<AnalyzerResult> AnalyzeProject(string projectPath, List<string> oldReferences, List<string> references)
        {
            var analyzerResult = await AnalyzeWithReferences(projectPath, oldReferences?.ToDictionary(r => projectPath, r => oldReferences), references?.ToDictionary(r => projectPath, r => references));
            return analyzerResult.FirstOrDefault();
        }

        ///<inheritdoc/>
        public override async Task<List<AnalyzerResult>> AnalyzeSolution(string solutionPath)
        {
            return await Analyze(solutionPath);
        }

        ///<inheritdoc/>
        public override async IAsyncEnumerable<AnalyzerResult> AnalyzeSolutionGeneratorAsync(string solutionPath)
        {
            var result = AnalyzeGeneratorAsync(solutionPath).GetAsyncEnumerator();
            try
            {
                while (await result.MoveNextAsync().ConfigureAwait(false))
                {
                    yield return result.Current;
                }
            }
            finally
            {
                await result.DisposeAsync();
            }
        }

        ///<inheritdoc/>
        public override async Task<List<AnalyzerResult>> AnalyzeSolution(string solutionPath, Dictionary<string, List<string>> oldReferences, Dictionary<string, List<string>> references)
        {
            var analyzerResults = await AnalyzeWithReferences(solutionPath, oldReferences, references);
            return analyzerResults;
        }

        private async Task<List<AnalyzerResult>> AnalyzeWithReferences(string path, Dictionary<string, List<string>> oldReferences, Dictionary<string, List<string>> references)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            List<ProjectWorkspace> workspaceResults = new List<ProjectWorkspace>();
            var analyzerResults = new List<AnalyzerResult>();

            WorkspaceBuilder builder = new WorkspaceBuilder(Logger, path, AnalyzerConfiguration);

            var projectBuildResults = builder.GenerateNoBuildAnalysis(oldReferences, references);

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
        private async IAsyncEnumerable<AnalyzerResult> AnalyzeGeneratorAsync(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            List<ProjectWorkspace> workspaceResults = new List<ProjectWorkspace>();

            WorkspaceBuilder builder = new WorkspaceBuilder(Logger, path, AnalyzerConfiguration);


            var projectBuildResultEnumerator = builder.BuildProject().GetAsyncEnumerator();
            try
            {

                while (await projectBuildResultEnumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var projectBuildResult = projectBuildResultEnumerator.Current;
                    var workspaceResult = AnalyzeProject(projectBuildResult);
                    workspaceResult.ProjectGuid = projectBuildResult.ProjectGuid;
                    workspaceResult.ProjectType = projectBuildResult.ProjectType;
                    workspaceResults.Add(workspaceResult);

                    if (AnalyzerConfiguration.MetaDataSettings.LoadBuildData)
                    {
                        yield return new AnalyzerResult() { ProjectResult = workspaceResult, ProjectBuildResult = projectBuildResult };
                    }
                    else
                    {
                        yield return new AnalyzerResult() { ProjectResult = workspaceResult };
                    }
                }
            }
            finally
            {
                await projectBuildResultEnumerator.DisposeAsync();
            }
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
                var fileAnalysis = AnalyzeFile(fileBuildResult, workspace.ProjectRootPath);
                workspace.SourceFileResults.Add(fileAnalysis);
            }

            return workspace;
        }

        private RootUstNode AnalyzeFile(SourceFileBuildResult sourceFileBuildResult, string projectRootPath)
        {
            CodeContext codeContext = new CodeContext(sourceFileBuildResult.PrePortSemanticModel,
                sourceFileBuildResult.SemanticModel,
                sourceFileBuildResult.SyntaxTree,
                projectRootPath,
                sourceFileBuildResult.SourceFilePath,
                AnalyzerConfiguration,
                Logger);

            Logger.LogDebug("Analyzing: " + sourceFileBuildResult.SourceFileFullPath);

            using CSharpRoslynProcessor processor = new CSharpRoslynProcessor(codeContext);

            var result = processor.Visit(codeContext.SyntaxTree.GetRoot());
            return result as RootUstNode;
        }

        public override async Task<AnalyzerResult> AnalyzeFile(string filePath, AnalyzerResult analyzerResult)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            var projectBuildResult = analyzerResult.ProjectBuildResult;
            var oldSourceFileResult = analyzerResult.ProjectResult.SourceFileResults.FirstOrDefault(sourceFile => sourceFile.FileFullPath == filePath);

            analyzerResult.ProjectResult.SourceFileResults.Remove(oldSourceFileResult);

            ProjectBuildHandler projectBuildHandler = new ProjectBuildHandler(Logger,
                analyzerResult.ProjectBuildResult.Project,
                analyzerResult.ProjectBuildResult.Compilation,
                analyzerResult.ProjectBuildResult.PrePortCompilation,
                AnalyzerConfiguration);

            analyzerResult.ProjectBuildResult = await projectBuildHandler.IncrementalBuild(filePath, analyzerResult.ProjectBuildResult);
            var newSourceFileBuildResult = projectBuildResult.SourceFileBuildResults.FirstOrDefault(sourceFile => sourceFile.SourceFileFullPath == filePath);

            var fileAnalysis = AnalyzeFile(newSourceFileBuildResult, analyzerResult.ProjectResult.ProjectRootPath);
            analyzerResult.ProjectResult.SourceFileResults.Add(fileAnalysis);

            return analyzerResult;
        }
        public override async Task<List<AnalyzerResult>> AnalyzeFile(string filePath, List<AnalyzerResult> analyzerResults)
        {
            var analyzerResult = analyzerResults.First(analyzerResults => analyzerResults.ProjectBuildResult.SourceFileBuildResults.Any(s => s.SourceFileFullPath == filePath));
            var updatedResult = await AnalyzeFile(filePath, analyzerResult);
            analyzerResults.Remove(analyzerResult);
            analyzerResults.Add(updatedResult);
            return analyzerResults;
        }
        public override async Task<IDEProjectResult> AnalyzeFile(string projectPath, string filePath, List<string> frameworkMetaReferences, List<string> coreMetaReferences)
        {
            var fileInfo = new Dictionary<string, string>();
            var content = File.ReadAllText(filePath);
            fileInfo.Add(filePath, content);
            return await AnalyzeFile(projectPath, fileInfo, frameworkMetaReferences, coreMetaReferences);
        }
        public override async Task<IDEProjectResult> AnalyzeFile(string projectPath, List<string> filePaths, List<string> frameworkMetaReferences, List<string> coreMetaReferences)
        {
            var fileInfo = new Dictionary<string, string>();
            filePaths.ForEach(filePath => {
                var content = File.ReadAllText(filePath);
                fileInfo.Add(filePath, content);
            });
            return await AnalyzeFile(projectPath, fileInfo, frameworkMetaReferences, coreMetaReferences);
        }
        public override async Task<IDEProjectResult> AnalyzeFile(string projectPath, string filePath, string fileContent, List<string> frameworkMetaReferences, List<string> coreMetaReferences)
        {
            var fileInfo = new Dictionary<string, string>();
            fileInfo.Add(filePath, fileContent);
            return await AnalyzeFile(projectPath, fileInfo, frameworkMetaReferences, coreMetaReferences);
        }
        public override async Task<IDEProjectResult> AnalyzeFile(string projectPath, Dictionary<string, string> fileInfo, List<string> frameworkMetaReferences, List<string> coreMetaReferences)
        {
            var result = new IDEProjectResult();

            FileBuildHandler fileBuildHandler = new FileBuildHandler(Logger, projectPath, fileInfo, frameworkMetaReferences, coreMetaReferences);
            var sourceFileResults = await fileBuildHandler.Build();

            result.SourceFileBuildResults = sourceFileResults;
            sourceFileResults.ForEach(sourceFileResult => {
                var fileAnalysis = AnalyzeFile(sourceFileResult, projectPath);
                result.RootNodes.Add(fileAnalysis);
            });

            return result;
        }
        public override async Task<IDEProjectResult> AnalyzeFile(string projectPath, Dictionary<string, string> fileInfo, IEnumerable<PortableExecutableReference> frameworkMetaReferences, List<PortableExecutableReference> coreMetaReferences)
        {
            var result = new IDEProjectResult();

            FileBuildHandler fileBuildHandler = new FileBuildHandler(Logger, projectPath, fileInfo, frameworkMetaReferences, coreMetaReferences);
            var sourceFileResults = await fileBuildHandler.Build();

            result.SourceFileBuildResults = sourceFileResults;
            sourceFileResults.ForEach(sourceFileResult => {
                var fileAnalysis = AnalyzeFile(sourceFileResult, projectPath);
                result.RootNodes.Add(fileAnalysis);
            });

            return result;
        }
    }
}
