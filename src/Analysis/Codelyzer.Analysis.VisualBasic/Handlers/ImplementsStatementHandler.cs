using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class ImplementsStatementHandler : UstNodeHandler
    {
        private ImplementsStatement Model { get => (ImplementsStatement)UstNode; }

        public ImplementsStatementHandler(CodeContext context,
            ImplementsStatementSyntax syntaxNode)
            : base(context, syntaxNode, new ImplementsStatement())
        {
            Model.Identifier = syntaxNode.ImplementsKeyword.ToString();
        }
    }
}
