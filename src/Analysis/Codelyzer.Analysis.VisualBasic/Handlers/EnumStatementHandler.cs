using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class EnumStatementHandler : UstNodeHandler
    {
        private EnumStatement Model { get => (EnumStatement)UstNode; }

        public EnumStatementHandler(CodeContext context,
            EnumStatementSyntax syntaxNode)
            : base(context, syntaxNode, new EnumStatement())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
