using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class AttributeListHandler : UstNodeHandler
    {
        private AttributeList Model { get => (AttributeList)UstNode; }

        public AttributeListHandler(CodeContext context,
            AttributeListSyntax syntaxNode)
            : base(context, syntaxNode, new AttributeList())
        {
            Model.Identifier = syntaxNode.Attributes.ToString();
        }
    }
}
