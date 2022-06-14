using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codelyzer.Analysis.Analyzer
{
    abstract class LanguageAnalyzerFactory
    {
        public abstract LanguageAnalyzer GetLanguageAnalyzer();
    }
}
