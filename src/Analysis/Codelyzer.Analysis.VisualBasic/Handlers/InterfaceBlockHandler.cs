using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class InterfaceBlockHandler : UstNodeHandler
    {
        private InterfaceBlock Model { get => (InterfaceBlock)UstNode; }

        public InterfaceBlockHandler(CodeContext context,
            InterfaceBlockSyntax syntaxNode)
            : base(context, syntaxNode, new InterfaceBlock())
        {
            var interfaceSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);
            Model.Identifier = syntaxNode.BlockStatement.Identifier.ToString();

            if (interfaceSymbol != null)
            {
                if (interfaceSymbol.BaseType != null)
                {
                    Model.BaseType = interfaceSymbol.BaseType.ToString();
                    Model.BaseTypeOriginalDefinition = GetBaseTypOriginalDefinition(interfaceSymbol);
                }

                Model.Reference.Namespace = GetNamespace(interfaceSymbol);
                Model.Reference.Assembly = GetAssembly(interfaceSymbol);
                Model.Reference.Version = GetAssemblyVersion(interfaceSymbol);
                Model.Reference.AssemblySymbol = interfaceSymbol.ContainingAssembly;
            }
        }
    }
}
