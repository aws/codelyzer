using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class BlockStatementHandler : UstNodeHandler
    {
        private BlockStatement BlockStatement { get => (BlockStatement)UstNode; }

        public BlockStatementHandler(CodeContext context, BlockSyntax syntaxNode)
            : base(context, syntaxNode, new BlockStatement())
        {
            BlockStatement.Identifier = "block";
        }
    }
}
