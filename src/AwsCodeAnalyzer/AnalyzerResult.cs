using AwsCodeAnalyzer.Build;
using AwsCodeAnalyzer.Model;
using System;

namespace AwsCodeAnalyzer
{
    public class AnalyzerResult : IDisposable
    {
        public ProjectWorkspace ProjectResult;
        public string OutputJsonFilePath;
        public ProjectBuildResult ProjectBuildResult;

        public void Dispose()
        {
            ProjectResult = null;
            ProjectBuildResult?.Dispose();
            ProjectBuildResult = null;
        }
    }
}