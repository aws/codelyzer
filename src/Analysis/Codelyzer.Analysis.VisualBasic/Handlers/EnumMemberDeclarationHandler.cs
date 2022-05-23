using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class EnumMemberDeclarationHandler : UstNodeHandler
    {
        private EnumMemberDeclaration Model { get => (EnumMemberDeclaration)UstNode; }

        public EnumMemberDeclarationHandler(CodeContext context,
            EnumMemberDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new EnumMemberDeclaration())
        {
            Model.Identifier = syntaxNode.Identifier.Text;
        }
    }
}
