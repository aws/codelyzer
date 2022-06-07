using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
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
                Model.SemanticReturnType = SemanticHelper.GetSemanticType(syntaxNode.Expression, SemanticModel, OriginalSemanticModel);
            }
        }
    }
}
