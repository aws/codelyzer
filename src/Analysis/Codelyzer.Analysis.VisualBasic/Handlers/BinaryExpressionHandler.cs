using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class BinaryExpressionHandler : UstNodeHandler
    {
        private BinaryExpression Model { get => (BinaryExpression)UstNode; }

        public BinaryExpressionHandler(CodeContext context,
            BinaryExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new BinaryExpression())
        {
            if (syntaxNode.OperatorToken != null)
                Model.Identifier = syntaxNode.OperatorToken.ValueText;

            Model.SemanticType = SemanticHelper.GetSemanticType(syntaxNode, SemanticModel, OriginalSemanticModel);
        }
    }
}
