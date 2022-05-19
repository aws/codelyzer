using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class EndBlockStatementHandler : UstNodeHandler
    {
        private EndBlockStatement Model { get => (EndBlockStatement)UstNode; }

        public EndBlockStatementHandler(CodeContext context,
            EndBlockStatementSyntax syntaxNode)
            : base(context, syntaxNode, new EndBlockStatement())
        {
            Model.Identifier = $"{syntaxNode.EndKeyword} {syntaxNode.BlockKeyword}";
        }
    }
}
