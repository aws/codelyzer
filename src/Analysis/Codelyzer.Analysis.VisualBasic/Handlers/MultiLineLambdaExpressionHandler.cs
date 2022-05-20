using System.Linq;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class MultiLineLambdaExpressionHandler : UstNodeHandler
    {
        private MultiLineLambdaExpression Model { get => (MultiLineLambdaExpression)UstNode; }

        public MultiLineLambdaExpressionHandler(CodeContext context, MultiLineLambdaExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new MultiLineLambdaExpression())
        {
            Model.Identifier = syntaxNode.ToString();
            SetMetaData(syntaxNode);
        }

        private void SetMetaData(MultiLineLambdaExpressionSyntax syntaxNode)
        {
            var nodeSymbolInfo = SemanticHelper.GetSymbolInfo(syntaxNode, SemanticModel);
            var methodSymbol  = nodeSymbolInfo == null ? null : ((SymbolInfo)nodeSymbolInfo).Symbol as IMethodSymbol;
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
