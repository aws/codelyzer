using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class AccessorStatementHandler : UstNodeHandler
    {
        private AccessorStatement Model { get => (AccessorStatement)UstNode; }

        public AccessorStatementHandler(CodeContext context,
            AccessorStatementSyntax syntaxNode)
            : base(context, syntaxNode, new AccessorStatement())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
