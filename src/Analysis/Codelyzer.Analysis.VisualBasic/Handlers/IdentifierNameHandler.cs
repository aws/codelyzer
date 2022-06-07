using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Linq;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class IdentifierNameHandler : UstNodeHandler
    {
        private static Type[] identifierNameTypes = new Type[] {
            typeof(MethodBlockSyntax),
            typeof(ConstructorBlockSyntax),
            typeof(ClassBlockSyntax),
            typeof(VariableDeclaratorSyntax),
            typeof(TypeArgumentListSyntax),
            typeof(TypeParameterListSyntax),
            typeof(ParameterSyntax),
            typeof(TypeArgumentListSyntax),
            typeof(ObjectCreationExpressionSyntax),
            typeof(QualifiedNameSyntax),
        };

        private DeclarationNode Model { get => (DeclarationNode)UstNode; }

        public IdentifierNameHandler(CodeContext context,
            IdentifierNameSyntax syntaxNode)
            : base(context, syntaxNode, new DeclarationNode())
        {
            if (identifierNameTypes.Contains(syntaxNode.Parent?.GetType()))
            {
                var type = SemanticHelper.GetSemanticType(syntaxNode, SemanticModel, OriginalSemanticModel);
                var symbol = SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);
                if (symbol != null)
                {
                    Model.Identifier = symbol.Name != null ? symbol.Name.Trim() : type;
                    Model.Reference.Namespace = GetNamespace(symbol);
                    Model.Reference.Assembly = GetAssembly(symbol);
                    Model.Reference.Version = GetAssemblyVersion(symbol);
                    Model.Reference.AssemblySymbol = symbol.ContainingAssembly;
                }
                else
                {
                    Model.Identifier = syntaxNode.Identifier.Text.Trim();
                }
            }
        }
    }
}
