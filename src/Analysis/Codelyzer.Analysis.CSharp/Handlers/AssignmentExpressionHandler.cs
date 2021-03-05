using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class AssignmentExpressionHandler : UstNodeHandler
    {
        private AssignmentExpression Model { get => (AssignmentExpression)UstNode; }

        public AssignmentExpressionHandler(CodeContext context,
            AssignmentExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new AssignmentExpression())
        {
            Model.Identifier = syntaxNode.ToString();
            Model.Left = syntaxNode.Left.ToString();
            Model.Right = syntaxNode.Right.ToString();
            Model.Operator = syntaxNode.OperatorToken.ValueText;
        }
    }
}
