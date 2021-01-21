using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class ReturnStatementHandler : UstNodeHandler
    {
        private ReturnStatement Model { get => (ReturnStatement)UstNode; }

        public ReturnStatementHandler(CodeContext context, 
            ReturnStatementSyntax syntaxNode)
            : base(context, syntaxNode, new ReturnStatement())
        {
            if (syntaxNode.Expression != null)
            {
                Model.Identifier = syntaxNode.Expression.ToString();
                Model.SemanticReturnType = SemanticHelper.GetSemanticType(syntaxNode.Expression, SemanticModel);
            }
        }
    }
}
