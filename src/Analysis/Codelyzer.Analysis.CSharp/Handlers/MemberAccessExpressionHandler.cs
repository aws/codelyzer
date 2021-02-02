using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class MemberAccessExpressionHandler : UstNodeHandler
    {
        private MemberAccess Model { get => (MemberAccess)UstNode; }

        public MemberAccessExpressionHandler(CodeContext context,
            MemberAccessExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new MemberAccess())
        {
            Model.Identifier = syntaxNode.ToString();
            Model.Name = syntaxNode.Name?.ToString();
            Model.Expression = syntaxNode.Expression?.ToString();
        }
    }
}
