using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class ModuleStatementHandler : UstNodeHandler
    {
        private ModuleStatement Model { get => (ModuleStatement)UstNode; }

        public ModuleStatementHandler(CodeContext context,
            ModuleStatementSyntax syntaxNode)
            : base(context, syntaxNode, new ModuleStatement())
        {
            Model.Identifier = syntaxNode.ToString();
        }
    }
}
