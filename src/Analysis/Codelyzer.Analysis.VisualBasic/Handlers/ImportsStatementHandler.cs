using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class ImportsStatementHandler : UstNodeHandler
    {
        private ImportsStatement Model { get => (ImportsStatement)UstNode; }

        public ImportsStatementHandler(CodeContext context,
            ImportsStatementSyntax syntaxNode)
            : base(context, syntaxNode, new ImportsStatement())
        {
            Model.Identifier = syntaxNode.ImportsKeyword.ToString();
        }
    }
}
