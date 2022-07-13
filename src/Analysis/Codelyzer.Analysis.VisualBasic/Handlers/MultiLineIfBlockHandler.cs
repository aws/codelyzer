using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Linq;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class MultiLineIfBlockHandler : UstNodeHandler
    {
        private MultiLineIfBlock Model { get => (MultiLineIfBlock)UstNode; }

        public MultiLineIfBlockHandler(CodeContext context,
            MultiLineIfBlockSyntax syntaxNode)
            : base(context, syntaxNode, new MultiLineIfBlock())
        {
            Model.Identifier = syntaxNode.Kind().ToString();
            Model.Modifiers = syntaxNode.IfStatement.ToString();
        }
    }
}