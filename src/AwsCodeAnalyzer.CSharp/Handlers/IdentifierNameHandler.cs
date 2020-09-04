using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace AwsCodeAnalyzer.CSharp.Handlers
{
    public class IdentifierNameHandler : UstNodeHandler
    {
        private static Type[] identifierNameTypes = new Type[] {
            typeof(MethodDeclarationSyntax),
            typeof(ClassDeclarationSyntax),
            typeof(VariableDeclarationSyntax),
            typeof(ParameterSyntax),
            typeof(ObjectCreationExpressionSyntax)};

        private DeclarationNode Model { get => (DeclarationNode)UstNode; }

        public IdentifierNameHandler(CodeContext context,
            IdentifierNameSyntax syntaxNode)
            : base(context, syntaxNode, new DeclarationNode())
        {
            if (identifierNameTypes.Contains(syntaxNode.Parent.GetType()))
            {
                var type = SemanticHelper.GetSemanticType(syntaxNode, SemanticModel);
                var symbolInfo = SemanticModel.GetSymbolInfo(syntaxNode);
                if (symbolInfo.Symbol != null)
                {
                    Model.Identifier = type;
                    Model.Reference.Namespace = GetNamespace(symbolInfo.Symbol);
                    Model.Reference.Assembly = GetAssembly(symbolInfo.Symbol);
                }
                else
                {
                    Model.Identifier = syntaxNode.Identifier.Text.Trim();
                }
            }
        }
    }
}