using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwsCodeAnalyzer.CSharp.Handlers
{
    public class LiteralExpressionHandler : UstNodeHandler
    {
        private LiteralExpression Model { get => (LiteralExpression)UstNode; }

        public LiteralExpressionHandler(CodeContext context, 
            LiteralExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new LiteralExpression())
        {
            if (syntaxNode.Token != null)
                Model.Identifier = syntaxNode.Token.ValueText;
            
            if (syntaxNode.Token != null && syntaxNode.Token.Value != null)
                Model.LiteralType = syntaxNode.Token.Value.GetType().ToString();
            
            Model.SemanticType = SemanticHelper.GetSemanticType(syntaxNode, SemanticModel);
        }
    }
}