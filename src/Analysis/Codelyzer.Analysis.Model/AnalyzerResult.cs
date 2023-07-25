using Codelyzer.Analysis.Model.Build;
using System;

namespace Codelyzer.Analysis.Model
{
    /// <summary>
    /// The result of a Project Analysis
    /// </summary>
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
