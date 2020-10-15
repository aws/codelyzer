using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System;

namespace Codelyzer.Analysis.Common
{
    public class CodeContext : IDisposable
    {
        public CodeContext(SemanticModel semanticModel, 
            SyntaxTree syntaxTree,
            string workspacePath,
            string sourceFilePath,
            AnalyzerConfiguration analyzerConfiguration,
            ILogger logger)
        {
            SemanticModel = semanticModel;
            SyntaxTree = syntaxTree;
            WorkspacePath = workspacePath;
            AnalyzerConfiguration = analyzerConfiguration;
            SourceFilePath = sourceFilePath;
            Logger = logger;
        }

        public SemanticModel SemanticModel { get; private set; }
        public SyntaxTree SyntaxTree { get; private set; }
        
        public string WorkspacePath { get; private set;  }
        
        public string SourceFilePath { get; private set; }
        
        public AnalyzerConfiguration AnalyzerConfiguration { get; private set; }
        
        public ILogger Logger { get; private set; }

        public void Dispose()
        {
            SemanticModel = null;
            SyntaxTree = null;
            Logger = null;
            AnalyzerConfiguration = null;
        }
    }
}
