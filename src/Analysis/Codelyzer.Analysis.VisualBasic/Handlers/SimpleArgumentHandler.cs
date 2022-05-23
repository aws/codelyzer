using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class SimpleArgumentHandler : UstNodeHandler
    {
        private AttributeArgument Model { get => (AttributeArgument)UstNode; }

        public SimpleArgumentHandler(CodeContext context,
            SimpleArgumentSyntax syntaxNode)
            : base(context, syntaxNode, new AttributeArgument())
        {
            Model.Identifier = syntaxNode.ToString();
            Model.ArgumentName = syntaxNode.NameColonEquals?.Name.ToString();
            Model.ArgumentExpression = syntaxNode.Expression.ToString();
        }

    }
}
