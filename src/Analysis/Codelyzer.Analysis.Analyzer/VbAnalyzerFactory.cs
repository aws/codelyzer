using Microsoft.Extensions.Logging;

namespace Codelyzer.Analysis.Analyzers
{
    public class VbAnalyzerFactory:LanguageAnalyzerFactory
    {
        protected readonly AnalyzerConfiguration _analyzerConfiguration;
        protected readonly ILogger _logger;
        public VbAnalyzerFactory(AnalyzerConfiguration analyzerConfiguration, ILogger logger)
        {
            _analyzerConfiguration = analyzerConfiguration;
            _logger = logger;
        }
        public override LanguageAnalyzer GetLanguageAnalyzer()
        {
            return new VbAnalyzer(_analyzerConfiguration, _logger);
        }
    }
}
