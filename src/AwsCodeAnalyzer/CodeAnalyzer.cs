using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace AwsCodeAnalyzer
{
    public abstract class CodeAnalyzer
    {
        protected readonly AnalyzerOptions AnalyzerOptions;
        protected readonly ILogger Logger;

        protected CodeAnalyzer(AnalyzerOptions options, ILogger logger)
        {
            AnalyzerOptions = options;
            Logger = logger;
        }
        
        public abstract Task<AnalyzerResult> AnalyzeProject(string projectPath);
        public abstract Task<List<AnalyzerResult>> AnalyzeSolution(string solutionPath);
    }
}