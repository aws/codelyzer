using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class SimpleAsClauseHandler : UstNodeHandler
    {
        private SimpleAsClause Model { get => (SimpleAsClause)UstNode; }

        public SimpleAsClauseHandler(CodeContext context,
            SimpleAsClauseSyntax syntaxNode)
            : base(context, syntaxNode, new SimpleAsClause())
        {
            Model.Identifier = syntaxNode.ToString();
            Model.Type = syntaxNode.Type.ToString();
        }
    }
}
