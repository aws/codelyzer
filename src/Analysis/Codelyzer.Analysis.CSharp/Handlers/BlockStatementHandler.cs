using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class BlockStatementHandler : UstNodeHandler
    {
        private static Type[] blockStatementParentTypes = new Type[] {
            typeof(MethodDeclarationSyntax),
            typeof(ConstructorDeclarationSyntax)};

        private BlockStatement BlockStatement { get => (BlockStatement)UstNode; }

        public BlockStatementHandler(CodeContext context, BlockSyntax syntaxNode)
            : base(context, syntaxNode, new BlockStatement())
        {
            //To maintain same behavior as previous versions, blocks are only handled when directly under the method
            if (blockStatementParentTypes.Contains(syntaxNode.Parent?.GetType()))
            {
                BlockStatement.Identifier = "block";
            }
        }
    }
}
