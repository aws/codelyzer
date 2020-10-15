using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
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
