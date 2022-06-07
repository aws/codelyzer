using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class InterfaceStatementHandler : UstNodeHandler
    {
        private InterfaceStatement Model { get => (InterfaceStatement)UstNode; }

        public InterfaceStatementHandler(CodeContext context,
            InterfaceStatementSyntax syntaxNode)
            : base(context, syntaxNode, new InterfaceStatement())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
