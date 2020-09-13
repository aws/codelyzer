using Serilog;

namespace AwsCodeAnalyzer
{
    public static class CodeAnalyzerFactory
    {
        public static CodeAnalyzer GetAnalyzer(AnalyzerConfiguration configuration, ILogger logger)
        {
            return new CSharpCodeAnalyzer(configuration, logger);
        }
    }
}