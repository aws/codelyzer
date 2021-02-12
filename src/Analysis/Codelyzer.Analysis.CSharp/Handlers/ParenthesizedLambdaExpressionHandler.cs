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
            Model.Identifier = syntaxNode.ToString();
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

            Model.Parameters = syntaxNode.ParameterList.Parameters.Select(parameterSyntax => 
            {
                var parameterSymbol = (IParameterSymbol)SemanticModel.GetDeclaredSymbol(parameterSyntax);

                return new Parameter 
                {
                    Name = parameterSyntax.Identifier.Text,
                    Type = parameterSymbol.Type.Name,
                    SemanticType = parameterSymbol.Type.ToString()
                };
            }).ToList();
        }
    }
}
