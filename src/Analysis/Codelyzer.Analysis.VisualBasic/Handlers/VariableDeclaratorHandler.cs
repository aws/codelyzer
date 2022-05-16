using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class VariableDeclaratorHandler : UstNodeHandler
    {
        private VariableDeclarator Model { get => (VariableDeclarator)UstNode; }

        public VariableDeclaratorHandler(CodeContext context,
            VariableDeclaratorSyntax syntaxNode)
            : base(context, syntaxNode, new VariableDeclarator())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
