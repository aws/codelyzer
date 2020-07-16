using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace AwsCodeAnalyzer
{
    public abstract class CodeAnalyzer
    {
        protected readonly AnalyzerConfiguration AnalyzerConfiguration;
        protected readonly ILogger Logger;

        protected CodeAnalyzer(AnalyzerConfiguration configuration, ILogger logger)
        {
            AnalyzerConfiguration = configuration;
            Logger = logger;
        }
        
        public abstract Task<AnalyzerResult> AnalyzeProject(string projectPath);
        public abstract Task<List<AnalyzerResult>> AnalyzeSolution(string solutionPath);
    }
}