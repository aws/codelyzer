using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwsCodeAnalyzer.Build
{
    public class SourceFileBuildResult
    {
        public SyntaxTree SyntaxTree { get; set; }
        public SemanticModel SemanticModel { get; set; }
        public string SourceFileFullPath { get; set; }
        public string SourceFilePath { get; set; }
        public SyntaxGenerator SyntaxGenerator { get; set; }
    }
}
