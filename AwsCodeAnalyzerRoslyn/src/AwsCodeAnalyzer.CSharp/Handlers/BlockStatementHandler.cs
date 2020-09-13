using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwsCodeAnalyzer.CSharp.Handlers
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