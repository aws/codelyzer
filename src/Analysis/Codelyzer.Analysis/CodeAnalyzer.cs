using System.Collections.Generic;
using System.Threading.Tasks;
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
    }
}
