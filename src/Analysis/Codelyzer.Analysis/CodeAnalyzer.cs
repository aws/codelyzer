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
    }
}
