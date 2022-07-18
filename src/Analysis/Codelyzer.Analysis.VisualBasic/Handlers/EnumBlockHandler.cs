using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class EnumBlockHandler : UstNodeHandler
    {
        private EnumBlock Model { get => (EnumBlock)UstNode; }

        public EnumBlockHandler(CodeContext context,
            EnumBlockSyntax syntaxNode)
            : base(context, syntaxNode, new EnumBlock())
        {
            Model.Identifier = syntaxNode.Kind().ToString();
            Model.Modifiers = syntaxNode.EnumStatement.Modifiers.ToString();
        }
    }
}
