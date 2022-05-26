using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class IfStatementHandler : UstNodeHandler
    {
        private IfStatement Model { get => (IfStatement)UstNode; }

        public IfStatementHandler(CodeContext context,
            IfStatementSyntax syntaxNode)
            : base(context, syntaxNode, new IfStatement())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
