using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwsCodeAnalyzer.CSharp.Handlers
{
    public class IdentifierNameHandler : UstNodeHandler
    {
        private DeclarationNode Model { get => (DeclarationNode)UstNode; }

        public IdentifierNameHandler(CodeContext context, 
            IdentifierNameSyntax syntaxNode)
            : base(context, syntaxNode, new DeclarationNode())
        {

            if (syntaxNode.Parent is MethodDeclarationSyntax
                    || syntaxNode.Parent is ClassDeclarationSyntax
                    || syntaxNode.Parent is VariableDeclarationSyntax
                    || syntaxNode.Parent is ParameterSyntax
                    || syntaxNode.Parent is ObjectCreationExpressionSyntax
                    )
            {
                var type = SemanticHelper.GetSemanticType(syntaxNode, SemanticModel);
                var symbolInfo = SemanticModel.GetSymbolInfo(syntaxNode);
                if (symbolInfo.Symbol != null)
                {
                    Model.Identifier = type;
                    Model.SemanticNamespace = symbolInfo.Symbol.ContainingNamespace.ToString().Trim();
                }
            }
        }
    }
}