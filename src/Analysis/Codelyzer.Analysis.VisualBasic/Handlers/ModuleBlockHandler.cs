using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Linq;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class ModuleBlockHandler : UstNodeHandler
    {
        private ModuleBlock Model { get => (ModuleBlock)UstNode; }

        public ModuleBlockHandler(CodeContext context,
            ModuleBlockSyntax syntaxNode)
            : base(context, syntaxNode, new ModuleBlock())
        {
            var classSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);

            Model.Identifier = syntaxNode.Kind().ToString();
            Model.Modifiers = syntaxNode.BlockStatement.Modifiers.ToString();

            if (classSymbol != null)
            {
                if (classSymbol.BaseType != null)
                {
                    Model.BaseType = classSymbol.BaseType.ToString();
                    Model.BaseTypeOriginalDefinition = GetBaseTypOriginalDefinition(classSymbol);
                    Model.Reference.Namespace = GetNamespace(classSymbol);
                    Model.Reference.Assembly = GetAssembly(classSymbol);
                    Model.Reference.Version = GetAssemblyVersion(classSymbol);
                    Model.Reference.AssemblySymbol = classSymbol.ContainingAssembly;
                }

                if (classSymbol.Interfaces != null)
                {
                    Model.BaseList = classSymbol.Interfaces.Select(x => x.ToString())?.ToList();
                }
            }
        }
    }
}
