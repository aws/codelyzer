using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class NamespaceStatementHandler : UstNodeHandler
    {
        private NamespaceStatement model { get => (NamespaceStatement)UstNode; }

        public NamespaceStatementHandler(CodeContext context, 
            NamespaceStatementSyntax syntaxNode)
            : base(context, syntaxNode, new NamespaceStatement())
        {
            model.Identifier = syntaxNode.Name.ToString();
        }

    }
}
