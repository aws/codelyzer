using Microsoft.CodeAnalysis;
using Serilog;

namespace AwsCodeAnalyzer.Common
{
    public class CodeContext
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
    }
}