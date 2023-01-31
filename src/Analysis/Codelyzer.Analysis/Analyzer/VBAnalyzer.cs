
using Codelyzer.Analysis.Build;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.VisualBasic;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Codelyzer.Analysis.Analyzer
{
    class VBAnalyzer : LanguageAnalyzer
    {
        public VBAnalyzer(AnalyzerConfiguration configuration, ILogger logger)
            : base(configuration, logger)
        {
        }
        public override string Language => LanguageOptions.Vb;
        public override string ProjectFilePath { set; get; }

        public override RootUstNode AnalyzeFile(SourceFileBuildResult sourceFileBuildResult, string projectRootPath)
        {
            CodeContext codeContext = new CodeContext(sourceFileBuildResult.PrePortSemanticModel,
                sourceFileBuildResult.SemanticModel,
                sourceFileBuildResult.SyntaxTree,
                projectRootPath,
                sourceFileBuildResult.SourceFilePath,
                AnalyzerConfiguration,
                Logger);

            Logger.LogDebug("Analyzing: " + sourceFileBuildResult.SourceFileFullPath);

            using VisualBasicRoslynProcessor processor = new VisualBasicRoslynProcessor(codeContext);

            var result = (RootUstNode) processor.Visit(codeContext.SyntaxTree.GetRoot());

            result.LinesOfCode = sourceFileBuildResult.SyntaxTree.GetRoot().DescendantTrivia()
                .Where(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

            return result as RootUstNode;
        }
    }
}
