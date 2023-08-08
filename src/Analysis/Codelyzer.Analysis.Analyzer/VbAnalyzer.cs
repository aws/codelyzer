using System.Linq;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Codelyzer.Analysis.Model.Build;
using Codelyzer.Analysis.VisualBasic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;

namespace Codelyzer.Analysis.Analyzers
{
    class VbAnalyzer : LanguageAnalyzer
    {
        public VbAnalyzer(AnalyzerConfiguration configuration, ILogger logger)
            : base(configuration, logger)
        {
        }
        public override string Language => LanguageOptions.Vb;
        public override string ProjectFilePath { set; get; }

        public override RootUstNode AnalyzeFile(SourceFileBuildResult sourceFileBuildResult, string projectRootPath)
        {
            var codeContext = new CodeContext(sourceFileBuildResult.PrePortSemanticModel,
                sourceFileBuildResult.SemanticModel,
                sourceFileBuildResult.SyntaxTree,
                projectRootPath,
                sourceFileBuildResult.SourceFilePath,
                AnalyzerConfiguration,
                Logger);

            Logger.LogDebug("Analyzing: " + sourceFileBuildResult.SourceFileFullPath);

            using (var processor = new VisualBasicRoslynProcessor(codeContext))
            {
                var result = (RootUstNode)processor.Visit(codeContext.SyntaxTree.GetRoot());

                result.LinesOfCode = sourceFileBuildResult.SyntaxTree.GetRoot()
                    .DescendantTrivia().Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
                
                return result;
            }

        }
    }
}
