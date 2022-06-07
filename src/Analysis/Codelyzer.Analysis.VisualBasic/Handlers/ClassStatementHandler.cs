using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class ClassStatementHandler : UstNodeHandler
    {
        private ClassStatement Model { get => (ClassStatement)UstNode; }

        public ClassStatementHandler(CodeContext context,
            ClassStatementSyntax syntaxNode)
            : base(context, syntaxNode, new ClassStatement())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
