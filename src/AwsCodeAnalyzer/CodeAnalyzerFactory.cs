using Serilog;

namespace AwsCodeAnalyzer
{
    public static class CodeAnalyzerFactory
    {
        public static CodeAnalyzer GetAnalyzer(AnalyzerOptions options, ILogger logger)
        {
            return new CSharpCodeAnalyzer(options, logger);
        }
    }
}