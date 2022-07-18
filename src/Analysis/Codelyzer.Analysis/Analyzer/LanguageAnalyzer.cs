using System;
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
