using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class ClassDeclarationHandler : UstNodeHandler
    {
        // Key: class full identifier, value: ClassDeclaration.
        private static Dictionary<string, ClassDeclaration> _classDelcarationCache = new();
        private ClassDeclaration ClassDeclaration { get => (ClassDeclaration)UstNode; }

        public ClassDeclarationHandler(CodeContext context,
            ClassDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new ClassDeclaration())
        {
            var classSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);
            if (classSymbol != null)
            {
                var fullIdentifier = classSymbol.OriginalDefinition.ToString();
                if (_classDelcarationCache.ContainsKey(fullIdentifier))
                {
                    UstNode = _classDelcarationCache[fullIdentifier];
                    return;
                }
                Set(ClassDeclaration, classSymbol);
            }
        }

        private void Set(ClassDeclaration classDeclaration, INamedTypeSymbol classSymbol)
        {
            var syntaxNodes = classSymbol.DeclaringSyntaxReferences;
            if (syntaxNodes.Length > 0)
            {
                var syntaxNode = (ClassDeclarationSyntax)syntaxNodes[0].GetSyntax();
                classDeclaration.Identifier = syntaxNode.Identifier.ToString();
                classDeclaration.Modifiers = syntaxNode.Modifiers.ToString();
            }

            if (classSymbol.BaseType != null)
            {
                classDeclaration.BaseType = classSymbol.BaseType.ToString();
                classDeclaration.BaseTypeOriginalDefinition = GetBaseTypOriginalDefinition(classSymbol);
                classDeclaration.Reference.Namespace = GetNamespace(classSymbol);
                classDeclaration.Reference.Assembly = GetAssembly(classSymbol);
                classDeclaration.Reference.Version = GetAssemblyVersion(classSymbol);
                classDeclaration.Reference.AssemblySymbol = classSymbol.ContainingAssembly;
                classDeclaration.FullIdentifier = classSymbol.OriginalDefinition.ToString();
            }

            if (classSymbol.Interfaces != null)
            {
                classDeclaration.BaseList = classSymbol.Interfaces.Select(x => x.ToString())?.ToList();
            }
            classDeclaration.BaseTypeDeclaration = GetBaseTypeDeclaration(classSymbol.BaseType);
        }

        private ClassDeclaration GetBaseTypeDeclaration(INamedTypeSymbol baseTypeSymbol)
        {
            if (baseTypeSymbol == null) return null;

            var fullIdentifier = baseTypeSymbol.OriginalDefinition.ToString();
            if (_classDelcarationCache.ContainsKey(fullIdentifier))
            {
                return _classDelcarationCache[fullIdentifier];
            }

            ClassDeclaration baseDeclaration = new();
            Set(baseDeclaration, baseTypeSymbol);

            return baseDeclaration;
        }
    }
}
