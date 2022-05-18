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
            var classSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);

            Model.Identifier = syntaxNode.ClassStatement.Identifier.ToString();
            Model.Modifiers = syntaxNode.ClassStatement.Modifiers.ToString();

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
