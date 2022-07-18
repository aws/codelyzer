using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codelyzer.Analysis
{
    public class SolutionAnalyzerResult
    {
        public List<AnalyzerResult> AnalyzerResults { get; set; }
        public CodeGraph CodeGraph { get; set; }
    }
}
