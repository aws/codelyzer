using System.Collections.Generic;
using System.Threading.Tasks;
using Codelyzer.Analysis.Build;
using Microsoft.Extensions.Logging;

namespace Codelyzer.Analysis
{
    /// <summary>
    /// Abstract class for implementing code analyzers
    /// </summary>
    public abstract class CodeAnalyzer
    {
        protected readonly AnalyzerConfiguration AnalyzerConfiguration;
        protected readonly ILogger Logger;

        protected CodeAnalyzer(AnalyzerConfiguration configuration, ILogger logger)
        {
            AnalyzerConfiguration = configuration;
            Logger = logger;
        }

        /// <summary>
        /// Runs analysis on a project
        /// </summary>
        /// <param name="projectPath">The path to the project file</param>
        /// <returns></returns>
        public abstract Task<AnalyzerResult> AnalyzeProject(string projectPath);

        /// <summary>
        /// Runs analysis on a solution
        /// </summary>
        /// <param name="solutionPath">The path to the solution file</param>
        /// <returns></returns>
        public abstract Task<List<AnalyzerResult>> AnalyzeSolution(string solutionPath);


        /// <summary>
        /// Analyzes a code file and adds it to an existing project analysis. If the file already exists, it replaces it in the result.
        /// </summary>
        /// <param name="filePath">The path to the code file</param>
        /// <param name="analyzerResult">The analyzer result to be modified</param>
        /// <returns></returns>
        public abstract Task<AnalyzerResult> AnalyzeFile(string filePath, AnalyzerResult analyzerResult);


        /// <summary>
        /// Analyzes a code file and adds it to an existing solution analysis.  If the file already exists, it replaces it in the result.
        /// </summary>
        /// <param name="filePath">The path to the code file</param>
        /// <param name="analyzerResults">The analyzer results to be modified</param>
        /// <returns></returns>
        public abstract Task<List<AnalyzerResult>> AnalyzeFile(string filePath, List<AnalyzerResult> analyzerResults);

        /// <summary>
        /// Analyzes a code file independently using the metareferences provided
        /// </summary>
        /// <param name="filePath">The path to the code files</param>
        /// <param name="frameworkMetaReferences">The references to be used when analyzing the file</param>
        /// <param name="coreMetaReferences">The references to be used when analyzing the file</param>
        /// <returns></returns>
        public abstract Task<IDEProjectResult> AnalyzeFile(string projectPath, string filePath, List<string> frameworkMetaReferences, List<string> coreMetaReferences);
        public abstract Task<IDEProjectResult> AnalyzeFile(string projectPath, string filePath, string fileContent, List<string> frameworkMetaReferences, List<string> coreMetaReferences);
        public abstract Task<IDEProjectResult> AnalyzeFile(string projectPath, List<string> filePath, List<string> frameworkMetaReferences, List<string> coreMetaReferences);
        public abstract Task<IDEProjectResult> AnalyzeFile(string projectPath, Dictionary<string, string> fileInfo, List<string> frameworkMetaReferences, List<string> coreMetaReferences);
    }
}
