using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class NamespaceDeclarationHandler : UstNodeHandler
    {
        private NamespaceDeclaration NamespaceDeclaration { get => (NamespaceDeclaration)UstNode; }

        public NamespaceDeclarationHandler(CodeContext context, 
            NamespaceDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new NamespaceDeclaration())
        {
            NamespaceDeclaration.Identifier = syntaxNode.Name.ToString();
            NamespaceDeclaration.FullIdentifier = syntaxNode.Name.ToString();
        }
        public NamespaceDeclarationHandler(CodeContext context,
       FileScopedNamespaceDeclarationSyntax syntaxNode)
       : base(context, syntaxNode, new NamespaceDeclaration())
        {
            NamespaceDeclaration.Identifier = syntaxNode.Name.ToString();
            NamespaceDeclaration.FullIdentifier = syntaxNode.Name.ToString();
        }

    }
}
