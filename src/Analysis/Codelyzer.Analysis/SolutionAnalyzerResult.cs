using System.Collections.Generic;
using Codelyzer.Analysis.Model;

namespace Codelyzer.Analysis
{
    public class SolutionAnalyzerResult
    {
        public List<AnalyzerResult> AnalyzerResults { get; set; }
        public CodeGraph CodeGraph { get; set; }
    }
}
