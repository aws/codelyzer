﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codelyzer.Analysis.Build;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Codelyzer.Analysis.Analyzer
{
    public abstract class LanguageAnalyzer
    {
        public abstract string Language { get; }
        public abstract string ProjectFilePath { get; set; }
        public abstract RootUstNode AnalyzeFile(SourceFileBuildResult sourceFileBuildResult, string projectRootPath);

        protected readonly AnalyzerConfiguration AnalyzerConfiguration;
        protected readonly ILogger Logger;

        public LanguageAnalyzer(AnalyzerConfiguration configuration, ILogger logger)
        {
            AnalyzerConfiguration = configuration;
            Logger = logger;
        }

        public  async Task<AnalyzerResult> AnalyzeProject(string projectPath)
        {
            AnalyzerResult analyzerResult = (await Analyze(projectPath)).First();
            return analyzerResult;
        }

        ///<inheritdoc/>
        public  async Task<List<AnalyzerResult>> AnalyzeSolution(string solutionPath)
        {
            //return await Analyze(solutionPath);
            var analyzerResults = await AnalyzeSolutionGeneratorAsync(solutionPath).ToListAsync();
            await GenerateOptionalOutput(analyzerResults);
            return analyzerResults;

        }

        ///<inheritdoc/>
        public  async IAsyncEnumerable<AnalyzerResult> AnalyzeSolutionGeneratorAsync(string solutionPath)
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
                        analyzerResult.ProjectResult.ProjectName + ".json", jsonOutput);
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

        public async Task<AnalyzerResult> AnalyzeFile(string filePath, AnalyzerResult analyzerResult)
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
        public  async Task<List<AnalyzerResult>> AnalyzeFile(string filePath, List<AnalyzerResult> analyzerResults)
        {
            var analyzerResult = analyzerResults.First(analyzerResults => analyzerResults.ProjectBuildResult.SourceFileBuildResults.Any(s => s.SourceFileFullPath == filePath));
            var updatedResult = await AnalyzeFile(filePath, analyzerResult);
            analyzerResults.Remove(analyzerResult);
            analyzerResults.Add(updatedResult);
            return analyzerResults;
        }
        public  async Task<IDEProjectResult> AnalyzeFile(string projectPath, string filePath, List<string> frameworkMetaReferences, List<string> coreMetaReferences)
        {
            var fileInfo = new Dictionary<string, string>();
            var content = File.ReadAllText(filePath);
            fileInfo.Add(filePath, content);
            return await AnalyzeFile(projectPath, fileInfo, frameworkMetaReferences, coreMetaReferences);
        }
        public  async Task<IDEProjectResult> AnalyzeFile(string projectPath, List<string> filePaths, List<string> frameworkMetaReferences, List<string> coreMetaReferences)
        {
            var fileInfo = new Dictionary<string, string>();
            filePaths.ForEach(filePath => {
                var content = File.ReadAllText(filePath);
                fileInfo.Add(filePath, content);
            });
            return await AnalyzeFile(projectPath, fileInfo, frameworkMetaReferences, coreMetaReferences);
        }
        public  async Task<IDEProjectResult> AnalyzeFile(string projectPath, string filePath, string fileContent, List<string> frameworkMetaReferences, List<string> coreMetaReferences)
        {
            var fileInfo = new Dictionary<string, string>();
            fileInfo.Add(filePath, fileContent);
            return await AnalyzeFile(projectPath, fileInfo, frameworkMetaReferences, coreMetaReferences);
        }
        public  async Task<IDEProjectResult> AnalyzeFile(string projectPath, Dictionary<string, string> fileInfo, List<string> frameworkMetaReferences, List<string> coreMetaReferences)
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
        public  async Task<IDEProjectResult> AnalyzeFile(string projectPath, Dictionary<string, string> fileInfo, IEnumerable<PortableExecutableReference> frameworkMetaReferences, List<PortableExecutableReference> coreMetaReferences)
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