using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwsCodeAnalyzer.CSharp.Handlers
{
    public class UsingDirectiveHandler : UstNodeHandler
    {
        private UsingDirective Model { get => (UsingDirective)UstNode; }

        public UsingDirectiveHandler(CodeContext context, 
            UsingDirectiveSyntax syntaxNode)
            : base(context, syntaxNode, new UsingDirective())
        {
            Model.Identifier = syntaxNode.Name.ToString();
        }
    }
}