using Codelyzer.Analysis.Build;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.CSharp;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Codelyzer.Analysis.Analyzer
{
    class CSharpAnalyzer : LanguageAnalyzer
    {
        public CSharpAnalyzer(AnalyzerConfiguration configuration, ILogger logger)
            : base(configuration, logger)
        {
        }
        public override string Language => LanguageOptions.CSharp;
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

            using CSharpRoslynProcessor processor = new CSharpRoslynProcessor(codeContext);

            var result = (RootUstNode) processor.Visit(codeContext.SyntaxTree.GetRoot());

            result.LinesOfCode = sourceFileBuildResult.SyntaxTree.GetRoot().DescendantTrivia()
                .Where(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

            return result as RootUstNode;
        }
    }
}
