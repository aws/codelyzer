using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class NamespaceBlockHandler : UstNodeHandler
    {
        private NamespaceBlock NamespaceBlock { get => (NamespaceBlock)UstNode; }

        public NamespaceBlockHandler(CodeContext context, 
            NamespaceBlockSyntax syntaxNode)
            : base(context, syntaxNode, new NamespaceBlock())
        {
            NamespaceBlock.Identifier = syntaxNode.NamespaceStatement.Name.ToString();
        }

    }
}
