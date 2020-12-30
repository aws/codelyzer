using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class ArrowExpressionClauseHandler : UstNodeHandler
    {
        private ArrowExpressionClause ArrowExpression { get => (ArrowExpressionClause)UstNode; }

        public ArrowExpressionClauseHandler(CodeContext context, ArrowExpressionClauseSyntax syntaxNode)
            : base(context, syntaxNode, new ArrowExpressionClause())
        {
            ArrowExpression.Identifier = "arrow-expression";
        }
    }
}
