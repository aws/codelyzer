using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class ElementAccessExpressionHandler : UstNodeHandler
    {
        private ElementAccess Model { get => (ElementAccess)UstNode; }

        public ElementAccessExpressionHandler(CodeContext context, 
            ElementAccessExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new ElementAccess())
        {
            Model.Identifier = syntaxNode.ToString();
            if (syntaxNode.Expression != null)
            {
                Model.Expression = syntaxNode.Expression.ToString();
            }
        }
    }
}
