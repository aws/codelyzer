using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Linq;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class SingleLineLambdaExpressionHandler : UstNodeHandler
    {
        private SingleLineLambdaExpression Model { get => (SingleLineLambdaExpression)UstNode; }

        public SingleLineLambdaExpressionHandler(CodeContext context, SingleLineLambdaExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new SingleLineLambdaExpression())
        {
            Model.Identifier = syntaxNode.ToString();
            SetMetaData(syntaxNode);
        }

        private void SetMetaData(SingleLineLambdaExpressionSyntax syntaxNode)
        {
            var nodeSymbolInfo = SemanticHelper.GetSymbolInfo(syntaxNode, SemanticModel);
            var methodSymbol = nodeSymbolInfo == null ? null : ((SymbolInfo)nodeSymbolInfo).Symbol as IMethodSymbol;
            if (methodSymbol != null)
            {
                Model.ReturnType = methodSymbol.ReturnType.Name;
                SemanticHelper.AddMethodProperties(methodSymbol, Model.SemanticProperties);
            }

            Model.Parameters = syntaxNode.SubOrFunctionHeader.ParameterList.Parameters.Select(parameterSyntax =>
            {
                var parameterSymbol = (IParameterSymbol)SemanticModel?.GetDeclaredSymbol(parameterSyntax);

                return new Parameter
                {
                    Name = parameterSyntax.Identifier?.Identifier.Text,
                    Type = parameterSymbol?.Type.Name,
                    SemanticType = parameterSymbol?.Type.ToString()
                };
            }).ToList();
        }
    }
}
