using AwsCodeAnalyzer.Build;
using AwsCodeAnalyzer.Model;

namespace AwsCodeAnalyzer
{
    public class AnalyzerResult
    {
        public ProjectWorkspace ProjectResult;
        public string OutputJsonFilePath;
        public ProjectBuildResult ProjectBuildResult;
    }
}