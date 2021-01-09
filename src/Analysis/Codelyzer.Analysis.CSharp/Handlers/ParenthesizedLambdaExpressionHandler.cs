using System.Linq;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class ParenthesizedLambdaExpressionHandler : UstNodeHandler
    {
        private ParenthesizedLambdaExpression Model { get => (ParenthesizedLambdaExpression)UstNode; }

        public ParenthesizedLambdaExpressionHandler(CodeContext context, ParenthesizedLambdaExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new ParenthesizedLambdaExpression())
        {
            Model.Identifier = "parenthesized-lambda-expression";
            SetMetaData(syntaxNode);
        }

        private void SetMetaData(ParenthesizedLambdaExpressionSyntax syntaxNode)
        {
            var nodeSymbolInfo = SemanticHelper.GetSymbolInfo(syntaxNode, SemanticModel);
            var methodSymbol  = nodeSymbolInfo == null ? null : ((SymbolInfo)nodeSymbolInfo).Symbol as IMethodSymbol;
            if (methodSymbol != null)
            {
                Model.ReturnType = methodSymbol.ReturnType.Name;
                SemanticHelper.AddMethodProperties(methodSymbol, Model.SemanticProperties);
            }

            Model.Parameters = syntaxNode.ParameterList.Parameters.Select(p => 
                new Parameter 
                {
                    Name = p.Identifier.Text,
                    Type = p.Type?.ToString(),
                    SemanticType = SemanticHelper.GetSemanticType(p.Type, SemanticModel)
                }
            ).ToList();
        }
    }
}
