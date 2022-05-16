using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class ClassBlockHandler : UstNodeHandler
    {
        private ClassBlock Model { get => (ClassBlock)UstNode; }

        public ClassBlockHandler(CodeContext context,
            ClassBlockSyntax syntaxNode)
            : base(context, syntaxNode, new ClassBlock())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
