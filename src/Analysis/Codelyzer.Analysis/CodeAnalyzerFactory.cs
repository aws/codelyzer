using Microsoft.Extensions.Logging;

namespace Codelyzer.Analysis
{
    /// <summary>
    /// Factory to create code analyzers based on a given configuration
    /// </summary>
    public static class CodeAnalyzerFactory
    {
        /// <summary>
        /// Initializes and return a new CodeAnalyzer
        /// </summary>
        /// <param name="configuration">Configuration of the analyzer</param>
        /// <param name="logger">Logger object</param>
        /// <returns></returns>
        public static CodeAnalyzer GetAnalyzer(AnalyzerConfiguration configuration, ILogger logger)
        {
            return new CSharpCodeAnalyzer(configuration, logger);
        }
    }
}
