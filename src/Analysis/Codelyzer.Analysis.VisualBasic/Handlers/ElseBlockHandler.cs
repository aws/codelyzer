using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class ElseBlockHandler : UstNodeHandler
    {
        private ElseBlock Model { get => (ElseBlock)UstNode; }

        public ElseBlockHandler(CodeContext context,
            ElseBlockSyntax syntaxNode)
            : base(context, syntaxNode, new ElseBlock())
        {
            Model.Identifier = syntaxNode.Kind().ToString();
        }
    }
}