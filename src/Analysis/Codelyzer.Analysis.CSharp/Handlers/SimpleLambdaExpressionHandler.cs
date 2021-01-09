using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class SimpleLambdaExpressionHandler : UstNodeHandler
    {
        private SimpleLambdaExpression Model { get => (SimpleLambdaExpression)UstNode; }

        public SimpleLambdaExpressionHandler(CodeContext context, SimpleLambdaExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new SimpleLambdaExpression())
        {
            Model.Identifier = "simple-lambda-expression";
            SetMetaData(syntaxNode);
        }

        private void SetMetaData(SimpleLambdaExpressionSyntax syntaxNode)
        {
            var nodeSymbolInfo = SemanticHelper.GetSymbolInfo(syntaxNode, SemanticModel);
            var methodSymbol  = nodeSymbolInfo == null ? null : ((SymbolInfo)nodeSymbolInfo).Symbol as IMethodSymbol;
            if (methodSymbol != null)
            {
                Model.ReturnType = methodSymbol.ReturnType.Name;
                SemanticHelper.AddMethodProperties(methodSymbol, Model.SemanticProperties);
            }
            
            var nodeParameter = syntaxNode.Parameter;
            Model.Parameter = new Parameter
            {
                Name = nodeParameter.Identifier.Text,
                Type = nodeParameter.Type?.ToString(),
                SemanticType = SemanticHelper.GetSemanticType(nodeParameter.Type, SemanticModel)
            };
        }
    }
}
