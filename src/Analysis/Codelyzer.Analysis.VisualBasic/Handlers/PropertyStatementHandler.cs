using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class PropertyStatementHandler : UstNodeHandler
    {
        private PropertyStatement Model { get => (PropertyStatement)UstNode; }

        public PropertyStatementHandler(CodeContext context,
            PropertyStatementSyntax syntaxNode)
            : base(context, syntaxNode, new PropertyStatement())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
