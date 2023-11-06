using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.CSharp;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Codelyzer.Analysis.Model.Build;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Codelyzer.Analysis.Analyzers
{
    class CSharpAnalyzer : LanguageAnalyzer
    {
        public CSharpAnalyzer(AnalyzerConfiguration configuration, ILogger logger)
            : base(configuration, logger) {}
        public override string Language => LanguageOptions.CSharp;
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
            //var a = JsonConvert.SerializeObject(sourceFileBuildResult.SemanticModel);
            Logger.LogDebug("Analyzing: " + sourceFileBuildResult.SourceFileFullPath);

            using (var processor = new CSharpRoslynProcessor(codeContext))
            {
                var result = (RootUstNode)processor.Visit(codeContext.SyntaxTree.GetRoot());

                result.LinesOfCode = sourceFileBuildResult.SyntaxTree.GetRoot()
                    .DescendantTrivia().Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia));

                return result as RootUstNode;
            }
        }
    }
}
