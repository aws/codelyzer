using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Codelyzer.Analysis.Model.Build
{
    public class SourceFileBuildResult
    {
        public SyntaxTree SyntaxTree { get; set; }
        public SemanticModel PrePortSemanticModel { get; set; }
        public SemanticModel SemanticModel { get; set; }
        public string SourceFileFullPath { get; set; }
        public string SourceFilePath { get; set; }
        public SyntaxGenerator SyntaxGenerator { get; set; }
    }
}
