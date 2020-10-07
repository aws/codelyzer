using Buildalyzer;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwsCodeAnalyzer.Build
{
    public class ProjectAnalysisResult : IDisposable
    {
        public Project Project { get; set; }
        public IAnalyzerResult AnalyzerResult { get; set; }
        public IProjectAnalyzer ProjectAnalyzer { get; set; }

        public void Dispose()
        {
            Project = null;
            AnalyzerResult = null;
            ProjectAnalyzer = null;
        }
    }
}
