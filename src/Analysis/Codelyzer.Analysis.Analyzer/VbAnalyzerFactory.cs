using Microsoft.Extensions.Logging;

namespace Codelyzer.Analysis.Analyzers
{
    public class VbAnalyzerFactory:LanguageAnalyzerFactory
    {
        protected readonly AnalyzerConfiguration AnalyzerConfiguration;
        protected readonly ILogger Logger;
        public VbAnalyzerFactory(AnalyzerConfiguration analyzerConfiguration, ILogger logger)
        {
            AnalyzerConfiguration = analyzerConfiguration;
            Logger = logger;
        }
        public override LanguageAnalyzer GetLanguageAnalyzer()
        {
            return new VbAnalyzer(AnalyzerConfiguration, Logger);
        }
    }
}
