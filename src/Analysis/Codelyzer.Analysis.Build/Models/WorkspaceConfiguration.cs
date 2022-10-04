using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;

namespace Codelyzer.Analysis.Build.Models
{
    public class WorkspaceConfiguration
    {
        public string Workspace { get; set; }
        public SolutionConfig Solution { get; set; }
    }

    public class SolutionConfig
    {
        public List<ProjectConfig> Projects { get; set; }
    }

    public class ProjectConfig
    {
        public string ProjectId { get; set; }
        public string AssemblyName { get; set; }
        public string Language { get; set; }
        public string FilePath { get; set; }
        public string OutputFilePath { get; set; }
        public List<DocumentConfig> Documents { get; set; }
        public List<string> ReferencedProjectIds { get; set; }
        public List<string> MetadataReferencesFilePath { get; set; }
        public List<string> AnalyzerReferencePaths { get; set; }
        public ParseOptions ParseOptions { get; set; }
        public CompilationOptions CompilationOptions { get; set; }
    }

    public class DocumentConfig
    {
        public string DocumentId { get; set; }
        public string AssemblyName { get; set; }
        public string FilePath { get; set; }
    }
}
