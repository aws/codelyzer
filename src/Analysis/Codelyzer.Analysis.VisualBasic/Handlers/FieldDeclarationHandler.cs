using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class FieldDeclarationHandler : UstNodeHandler
    {
        private FieldDeclaration Model { get => (FieldDeclaration)UstNode; }

        public FieldDeclarationHandler(CodeContext context,
            FieldDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new FieldDeclaration())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
