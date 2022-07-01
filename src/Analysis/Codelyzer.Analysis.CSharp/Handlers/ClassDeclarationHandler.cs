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
        private Dictionary<string, ClassDeclaration> _classDeclarationCache = new();
        private ClassDeclaration ClassDeclaration { get => (ClassDeclaration)UstNode; }

        public ClassDeclarationHandler(CodeContext context,
            ClassDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new ClassDeclaration())
        {
            var classSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);
            if (classSymbol != null)
            {
                var fullIdentifier = GetFullIdentifier(classSymbol);
                if (_classDeclarationCache.ContainsKey(fullIdentifier))
                {
                    UstNode = _classDeclarationCache[fullIdentifier];
                    return;
                }
                Set(ClassDeclaration, classSymbol);
                _classDeclarationCache.Add(fullIdentifier, ClassDeclaration);
            }
        }

        private void Set(ClassDeclaration classDeclaration, INamedTypeSymbol classSymbol)
        {
            classDeclaration.Identifier = classSymbol.Name;
            classDeclaration.FullIdentifier = classSymbol.OriginalDefinition.ToString();
            classDeclaration.Reference.Namespace = GetNamespace(classSymbol);
            classDeclaration.Reference.Assembly = GetAssembly(classSymbol);
            classDeclaration.Reference.Version = GetAssemblyVersion(classSymbol);
            classDeclaration.Reference.AssemblySymbol = classSymbol.ContainingAssembly;

            var syntaxNode = (ClassDeclarationSyntax)classSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (syntaxNode != null)
            {
                classDeclaration.Modifiers = syntaxNode.Modifiers.ToString();
            }

            if (classSymbol.BaseType != null)
            {
                classDeclaration.BaseType = classSymbol.BaseType.ToString();
                classDeclaration.BaseTypeOriginalDefinition = GetBaseTypOriginalDefinition(classSymbol);
                classDeclaration.BaseTypeDeclaration = GetBaseTypeDeclaration(classSymbol.BaseType);
            }

            if (classSymbol.AllInterfaces != null)
            {
                classDeclaration.BaseList = classSymbol.AllInterfaces.Select(x => x.ToString())?.ToList();
            }
        }

        private ClassDeclaration GetBaseTypeDeclaration(INamedTypeSymbol baseTypeSymbol)
        {
            var fullIdentifier = GetFullIdentifier(baseTypeSymbol);
            if (_classDeclarationCache.ContainsKey(fullIdentifier))
            {
                return _classDeclarationCache[fullIdentifier];
            }

            ClassDeclaration baseDeclaration = new();
            Set(baseDeclaration, baseTypeSymbol);
            _classDeclarationCache.Add(fullIdentifier, baseDeclaration);    

            return baseDeclaration;
        }
    }
}
