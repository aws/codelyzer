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
            Model.Identifier = null;
            SetMetaData(syntaxNode);
        }

        private void SetMetaData(ReturnStatementSyntax syntaxNode)
        {
            if (syntaxNode.Expression != null)
                Model.SemanticReturnType = SemanticHelper.GetSemanticType(syntaxNode.Expression, SemanticModel);
        }
    }
}
