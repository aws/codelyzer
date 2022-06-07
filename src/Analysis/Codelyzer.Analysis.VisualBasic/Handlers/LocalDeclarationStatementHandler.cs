using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class LocalDeclarationStatementHandler : UstNodeHandler
    {
        private LocalDeclarationStatement Model { get => (LocalDeclarationStatement)UstNode; }

        public LocalDeclarationStatementHandler(CodeContext context,
            LocalDeclarationStatementSyntax syntaxNode)
            : base(context, syntaxNode, new LocalDeclarationStatement())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
