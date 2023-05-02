using System.Collections.Generic;

namespace Codelyzer.Analysis.Model
{
    public class SolutionAnalyzerResult
    {
        public List<AnalyzerResult> AnalyzerResults { get; set; }
        public CodeGraph.CodeGraph CodeGraph { get; set; }
    }
}
