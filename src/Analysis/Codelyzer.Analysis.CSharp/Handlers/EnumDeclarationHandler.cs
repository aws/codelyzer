using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class EnumDeclarationHandler : UstNodeHandler
    {
        private EnumDeclaration EnumDeclaration { get => (EnumDeclaration)UstNode; }

        public EnumDeclarationHandler(CodeContext context,
            EnumDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new EnumDeclaration())
        {
            var EnumSymbol = SemanticModel.GetDeclaredSymbol(syntaxNode);
            EnumDeclaration.Identifier = syntaxNode.Identifier.ToString();

            if (EnumSymbol != null)
            {
                EnumDeclaration.Reference.Namespace = GetNamespace(EnumSymbol);
                EnumDeclaration.Reference.Assembly = GetAssembly(EnumSymbol);
                EnumDeclaration.Reference.AssemblySymbol = EnumSymbol.ContainingAssembly;
            }
        }
    }
}
