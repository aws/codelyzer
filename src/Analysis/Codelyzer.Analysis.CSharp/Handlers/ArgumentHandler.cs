using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class ArgumentHandler : UstNodeHandler
    {
        private Argument Model { get => (Argument)UstNode; }

        public ArgumentHandler(CodeContext context,
            ArgumentSyntax syntaxNode)
            : base(context, syntaxNode, new Argument())
        {
            Model.Identifier = syntaxNode.Expression.ToString();
            Model.SemanticType = SemanticHelper.GetSemanticType(syntaxNode.Expression, SemanticModel);
        }
    }
}
