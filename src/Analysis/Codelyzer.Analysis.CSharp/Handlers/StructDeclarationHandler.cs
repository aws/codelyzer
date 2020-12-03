using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class StructDeclarationHandler : UstNodeHandler
    {
        private StructDeclaration StructDeclaration { get => (StructDeclaration)UstNode; }

        public StructDeclarationHandler(CodeContext context,
            StructDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new StructDeclaration())
        {
            var structSymbol = SemanticModel.GetDeclaredSymbol(syntaxNode);
            StructDeclaration.Identifier = syntaxNode.Identifier.ToString();

            if (structSymbol != null)
            {
                StructDeclaration.Reference.Namespace = GetNamespace(structSymbol);
                StructDeclaration.Reference.Assembly = GetAssembly(structSymbol);
                StructDeclaration.Reference.AssemblySymbol = structSymbol.ContainingAssembly;
            }
        }
    }
}
