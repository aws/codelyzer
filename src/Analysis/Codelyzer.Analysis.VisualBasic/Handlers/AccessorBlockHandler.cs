using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Linq;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class AccessorBlockHandler : UstNodeHandler
    {
        private AccessorBlock Model { get => (AccessorBlock)UstNode; }

        public AccessorBlockHandler(CodeContext context,
            AccessorBlockSyntax syntaxNode)
            : base(context, syntaxNode, new AccessorBlock())
        {
            Model.Identifier = syntaxNode.Kind().ToString();
            Model.Modifiers = syntaxNode.AccessorStatement.Modifiers.ToString();
        }
    }
}
