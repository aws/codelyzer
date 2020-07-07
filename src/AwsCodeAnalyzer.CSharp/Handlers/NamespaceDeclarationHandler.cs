using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwsCodeAnalyzer.CSharp.Handlers
{
    public class NamespaceDeclarationHandler : UstNodeHandler
    {
        private NamespaceDeclaration NamespaceDeclaration { get => (NamespaceDeclaration)UstNode; }

        public NamespaceDeclarationHandler(CodeContext context, 
            NamespaceDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new NamespaceDeclaration())
        {
            NamespaceDeclaration.Identifier = syntaxNode.Name.ToString();
        }

    }
}