using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Codelyzer.Analysis.Analyzers;
using Codelyzer.Analysis.Build;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Codelyzer.Analysis.Model.Build;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace Codelyzer.Analysis.Analyzer
{
    public class CodeAnalyzerByLanguage
    {
        protected readonly AnalyzerConfiguration AnalyzerConfiguration;
        protected readonly ILogger Logger;

        private readonly Analyzers.CodeAnalyzer _codeAnalyzer;

        public CodeAnalyzerByLanguage(AnalyzerConfiguration configuration, ILogger logger)
        {
            AnalyzerConfiguration = configuration;
            Logger = logger;
            _codeAnalyzer = new Analyzers.CodeAnalyzer(configuration, logger);
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

        public async Task<List<AnalyzerResult>> AnalyzeSolutionGenerator(string solutionPath)
        {
            var analyzerResults = await AnalyzeSolutionGeneratorAsync(solutionPath).ToListAsync();

            await _codeAnalyzer.GenerateOptionalOutput(analyzerResults);
            return analyzerResults;
        }

        ///<inheritdoc/>
        public async IAsyncEnumerable<AnalyzerResult> AnalyzeSolutionGeneratorAsync(string solutionPath)
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

        private async IAsyncEnumerable<AnalyzerResult> AnalyzeGeneratorAsync(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }
            WorkspaceBuilder builder = new WorkspaceBuilder(Logger, path, AnalyzerConfiguration);
            var projectBuildResultEnumerator = builder.BuildProject().GetAsyncEnumerator();
            try
            {
                while (await projectBuildResultEnumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var projectBuildResult = projectBuildResultEnumerator.Current;
                    yield return await _codeAnalyzer.AnalyzeProjectBuildResult(projectBuildResult);
                }
            }
            finally
            {
                await projectBuildResultEnumerator.DisposeAsync();
            }
        }

        public async Task<List<AnalyzerResult>> Analyze(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            List<ProjectWorkspace> workspaceResults = new List<ProjectWorkspace>();

            WorkspaceBuilder builder = new WorkspaceBuilder(Logger, path, AnalyzerConfiguration);
            var projectBuildResults = await builder.Build();

            var analyzerResults = await _codeAnalyzer.Analyze(projectBuildResults);
            return analyzerResults;
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
            return await _codeAnalyzer.AnalyzeFile(filePath, analyzerResult.ProjectBuildResult, analyzerResult);
        }

        ///<inheritdoc/>
        public async Task<SolutionAnalyzerResult> AnalyzeSolutionWithGraph(string solutionPath)
        {
            var analyzerResults = await AnalyzeSolution(solutionPath);
            var codeGraph = GenerateGraph(analyzerResults);

            return new SolutionAnalyzerResult()
            {
                CodeGraph = codeGraph,
                AnalyzerResults = analyzerResults
            };
        }

        public async Task<SolutionAnalyzerResult> AnalyzeSolutionGeneratorWithGraph(string solutionPath)
        {
            var analyzerResults = await AnalyzeSolutionGenerator(solutionPath);
            var codeGraph = GenerateGraph(analyzerResults);

            return new SolutionAnalyzerResult()
            {
                CodeGraph = codeGraph,
                AnalyzerResults = analyzerResults
            };
        }

        ///<inheritdoc/>
        public CodeGraph GenerateGraph(List<AnalyzerResult> analyzerResults)
        {

            var codeGraph = new CodeGraph(Logger);
            try
            {
                codeGraph.Initialize(analyzerResults);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while generating graph");
            }
            return codeGraph;
        }

        ///<inheritdoc/>
        public async Task<List<AnalyzerResult>> AnalyzeSolution(
            string solutionPath,
            Dictionary<string, List<string>> oldReferences,
            Dictionary<string, List<string>> references)
        {
            var analyzerResults = await AnalyzeWithReferences(solutionPath, oldReferences, references);
            return analyzerResults;
        }

        private async Task<List<AnalyzerResult>> AnalyzeWithReferences(
            string path,
            Dictionary<string, List<string>> oldReferences,
            Dictionary<string, List<string>> references)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }
            WorkspaceBuilder builder = new WorkspaceBuilder(Logger, path, AnalyzerConfiguration);
            var projectBuildResults = builder.GenerateNoBuildAnalysis(oldReferences, references);
            var analyzerResults = await _codeAnalyzer.Analyze(projectBuildResults);
            await _codeAnalyzer.GenerateOptionalOutput(analyzerResults);
            return analyzerResults;
        }

        public async Task<AnalyzerResult> AnalyzeProject(
            string projectPath,
            List<string> oldReferences,
            List<string> references)
        {
            var analyzerResult = await AnalyzeWithReferences(
                projectPath,
                oldReferences?.ToDictionary(r => projectPath, r => oldReferences),
                references?.ToDictionary(r => projectPath, r => references));
            return analyzerResult.FirstOrDefault();
        }


        //maintained for backwards compatibility
        public Analyzers.LanguageAnalyzer GetLanguageAnalyzerByProjectType(string projType)
        {
            return _codeAnalyzer.GetLanguageAnalyzerByProjectType(projType);
        }

        /// <summary>
        /// Deprecated method. Call directly into AnalyzeFile instead. Returns language analyzer based on file extension.
        /// </summary>
        /// <param name="fileType">File extension, either .cs or .vb</param>
        /// <returns>Language analyzer object for the corresponding language</returns>
        public LanguageAnalyzer GetLanguageAnalyzerByFileType(string fileType)
        {
            return _codeAnalyzer.GetLanguageAnalyzerByFileType(fileType);
        }

        public async Task<List<AnalyzerResult>> AnalyzeSolutionLiteBuildAsync(string solutionPath, Dictionary<string, List<string>> references)
        {
            var analyzerResults = await AnalyzeWithLiteBuild(solutionPath, references);
            return analyzerResults;
        }

        private async Task<List<AnalyzerResult>> AnalyzeWithLiteBuild(string path, Dictionary<string, List<string>> references)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            WorkspaceBuilder builder = new WorkspaceBuilder(Logger, path, AnalyzerConfiguration);

            var watch = new Stopwatch();
            watch.Start();
            var projectBuildResults = builder.BuildLiteBuildAnalysis(references);
            watch.Stop();
            Console.WriteLine($"Total time for building solution level compilation object and adHocWorkspace {watch.ElapsedMilliseconds / 1000} seconds");
            return await AnalyzeBuildResults(projectBuildResults);
        }

        private async Task<List<AnalyzerResult>> AnalyzeBuildResults(List<ProjectBuildResult> projectBuildResults)
        {
            var analyzerResults = new List<AnalyzerResult>();
            List<ProjectWorkspace> workspaceResults = new List<ProjectWorkspace>();
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

            return analyzerResults;
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
    }
}
