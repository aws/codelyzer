using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codelyzer.Analysis.Analyzer
{
    class VBAnalyerFactory:LanguageAnalyzerFactory
    {
        protected readonly AnalyzerConfiguration _analyzerConfiguration;
        protected readonly ILogger _logger;
        public VBAnalyerFactory(AnalyzerConfiguration analyzerConfiguration, ILogger logger)
        {
            _analyzerConfiguration = analyzerConfiguration;
            _logger = logger;
        }
        public override LanguageAnalyzer GetLanguageAnalyzer()
        {
            return new VBAnalyzer(_analyzerConfiguration, _logger);
        }
    }
}
