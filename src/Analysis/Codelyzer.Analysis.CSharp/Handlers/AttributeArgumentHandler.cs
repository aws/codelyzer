using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class AttributeArgumentHandler : UstNodeHandler
    {
        private AttributeArgument Model { get => (AttributeArgument)UstNode; }

        public AttributeArgumentHandler(CodeContext context,
            AttributeArgumentSyntax syntaxNode)
            : base(context, syntaxNode, new AttributeArgument())
        {
            Model.Identifier = syntaxNode.ToString();
            Model.ArgumentName = syntaxNode.NameEquals?.Name.ToString();
            Model.ArgumentExpression = syntaxNode.Expression.ToString();
        }
    }
}
