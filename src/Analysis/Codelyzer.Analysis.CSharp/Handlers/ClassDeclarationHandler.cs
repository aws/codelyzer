using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class ClassDeclarationHandler : UstNodeHandler
    {
        private ClassDeclaration ClassDeclaration { get => (ClassDeclaration)UstNode; }

        public ClassDeclarationHandler(CodeContext context,
            ClassDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new ClassDeclaration())
        {
            var classSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);

            ClassDeclaration.Identifier = syntaxNode.Identifier.ToString();
            ClassDeclaration.Modifiers = syntaxNode.Modifiers.ToString();

            if (classSymbol != null && classSymbol.BaseType != null)
            {
                ClassDeclaration.BaseType = classSymbol.BaseType.ToString();
                ClassDeclaration.BaseTypeOriginalDefinition = GetBaseTypOriginalDefinition(classSymbol); 
                ClassDeclaration.Reference.Namespace = GetNamespace(classSymbol);
                ClassDeclaration.Reference.Assembly = GetAssembly(classSymbol);
                ClassDeclaration.Reference.Version = GetAssemblyVersion(classSymbol);
                ClassDeclaration.Reference.AssemblySymbol = classSymbol.ContainingAssembly;
            }
        }
    }
}
