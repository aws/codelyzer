using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class IdentifierNameHandler : UstNodeHandler
    {
        private static Type[] identifierNameTypes = new Type[] {
            typeof(MethodDeclarationSyntax),
            typeof(ConstructorDeclarationSyntax),
            typeof(ClassDeclarationSyntax),
            typeof(VariableDeclarationSyntax),
            typeof(TypeArgumentListSyntax),
            typeof(TypeParameterListSyntax),
            typeof(ParameterSyntax),
            typeof(TypeArgumentListSyntax),
            typeof(ObjectCreationExpressionSyntax),
            typeof(QualifiedNameSyntax),
            typeof(CastExpressionSyntax),
            typeof(PropertyDeclarationSyntax)
        };

        private DeclarationNode Model { get => (DeclarationNode)UstNode; }

        public IdentifierNameHandler(CodeContext context,
            IdentifierNameSyntax syntaxNode)
            : base(context, syntaxNode, new DeclarationNode())
        {
            var type = SemanticHelper.GetSemanticType(syntaxNode, SemanticModel, OriginalSemanticModel);
            var symbol = SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);

            if (!string.IsNullOrEmpty(type) && symbol?.Kind == SymbolKind.NamedType)
            {
                Model.Identifier = symbol.Name != null ? symbol.Name.Trim() : type;
                Model.Reference.Namespace = GetNamespace(symbol);
                Model.Reference.Assembly = GetAssembly(symbol);
                Model.Reference.Version = GetAssemblyVersion(symbol);
                Model.Reference.AssemblySymbol = symbol.ContainingAssembly;
                Model.FullIdentifier = string.Concat(Model.Reference.Namespace, ".", Model.Identifier);
            }
            // In case we weren't able to get a semantic model, we will use the parent nodes as our guide
            else if (identifierNameTypes.Contains(syntaxNode.Parent?.GetType()))
            {
                if (!IsUsingStatementMember(syntaxNode))
                {
                    Model.Identifier = syntaxNode.Identifier.Text.Trim();
                    Model.FullIdentifier = Model.Identifier;
                }
            }
        }

        private bool IsUsingStatementMember(SyntaxNode syntaxNode)
        {
            if(syntaxNode == null)
            {
                return false;
            }
            if(syntaxNode is UsingDirectiveSyntax)
            {
                return true;
            }
            else
            {
                return IsUsingStatementMember(syntaxNode.Parent);
            }
        }
    }
}
