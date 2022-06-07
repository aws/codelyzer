using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class ElseStatementHandler : UstNodeHandler
    {
        private ElseStatement Model { get => (ElseStatement)UstNode; }

        public ElseStatementHandler(CodeContext context,
            ElseStatementSyntax syntaxNode)
            : base(context, syntaxNode, new ElseStatement())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
