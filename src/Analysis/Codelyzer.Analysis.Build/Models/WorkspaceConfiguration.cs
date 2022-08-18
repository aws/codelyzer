using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;

namespace Codelyzer.Analysis.Build.Models
{
    public class WorkspaceConfiguration
    {
        public string workspace { get; set; }
        public SolutionConfig solution { get; set; }
    }

    public class SolutionConfig
    {
        public List<ProjectConfig> projects { get; set; }
    }

    public class ProjectConfig
    {
        public string projectId { get; set; }
        public string assemblyName { get; set; }
        public string language { get; set; }
        public string filePath { get; set; }
        public string outputFilePath { get; set; }
        public List<DocumentConfig> documents { get; set; }
        public List<string> projectReferences { get; set; }
        public List<String> metadataReferencesFilePath { get; set; }
        public List<AnalyzerReference> analyzerReferences { get; set; }
        public ParseOptions parseOptions { get; set; }
        public CompilationOptions compilationOptions { get; set; }
    }

    public class DocumentConfig
    {
        public string documentId { get; set; }
        public string assemblyName { get; set; }
        public string filePath { get; set; }
    }
}
