using System;
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
        public static CodeAnalyzer GetAnalyzer(AnalyzerConfiguration configuration, ILogger logger, string projectFile = "")
        {
            if (configuration.MetaDataSettings.GenerateBinFiles)
            {
                // buildalyzer can't handle bin generation opion 
                configuration.MetaDataSettings.GenerateBinFiles = false;
            }
            if (configuration.Language == LanguageOptions.Vb ||projectFile.EndsWith(".vbproj",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new VisualBasicCodeAnalyzer(configuration, logger);
            }
            return new CSharpCodeAnalyzer(configuration, logger);
        }
    }
}
