using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codelyzer.Analysis.Analyzers
{
    public class CSharpAnalyzerFactory : LanguageAnalyzerFactory
    {
        protected readonly AnalyzerConfiguration AnalyzerConfiguration;
        protected readonly ILogger Logger;

        public CSharpAnalyzerFactory(AnalyzerConfiguration analyzerConfiguration, ILogger logger)
        {
            AnalyzerConfiguration = analyzerConfiguration;
            Logger = logger;
        }
        public override LanguageAnalyzer GetLanguageAnalyzer()
        {
            return new CSharpAnalyzer(AnalyzerConfiguration, Logger);
        }
    }

    
}
