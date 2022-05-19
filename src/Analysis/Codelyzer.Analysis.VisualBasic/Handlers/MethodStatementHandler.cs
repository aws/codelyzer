using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class MethodStatementHandler : UstNodeHandler
    {
        private MethodStatement Model { get => (MethodStatement)UstNode; }

        public MethodStatementHandler(CodeContext context,
            MethodStatementSyntax syntaxNode)
            : base(context, syntaxNode, new MethodStatement())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
