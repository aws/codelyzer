using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwsCodeAnalyzer.CSharp.Handlers
{
    public class AttributeHandler : UstNodeHandler
    {
        private Annotation Model { get => (Annotation)UstNode; }

        public AttributeHandler(CodeContext context, 
            AttributeSyntax syntaxNode)
            : base(context, syntaxNode, new Annotation())
        {
            Model.Identifier = syntaxNode.Name.ToString();

            var symbolInfo = SemanticModel.GetSymbolInfo(syntaxNode);
            if (symbolInfo.Symbol != null && symbolInfo.Symbol.ContainingNamespace != null)
            {
                Model.SemanticNamespace = symbolInfo.Symbol.ContainingNamespace.ToString().Trim();
            }
        }
    }
}