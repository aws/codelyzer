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
        private ClassDeclaration ClassDeclaration { get => (ClassDeclaration)UstNode; }

        public ClassDeclarationHandler(CodeContext context,
            ClassDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new ClassDeclaration())
        {
            var classSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);

            ClassDeclaration.Identifier = syntaxNode.Identifier.ToString();
            ClassDeclaration.Modifiers = syntaxNode.Modifiers.ToString();

            if (classSymbol != null)
            {
                ClassDeclaration.FullIdentifier = classSymbol.OriginalDefinition.ToString();
                ClassDeclaration.Reference.Namespace = GetNamespace(classSymbol);
                ClassDeclaration.Reference.Assembly = GetAssembly(classSymbol);
                ClassDeclaration.Reference.Version = GetAssemblyVersion(classSymbol);
                ClassDeclaration.Reference.AssemblySymbol = classSymbol.ContainingAssembly;

                ClassDeclaration.BaseList = new();
                if (classSymbol.BaseType != null)
                {
                    var baseTypeSymbol = classSymbol.BaseType;
                    ClassDeclaration.BaseType = baseTypeSymbol.ToString();
                    ClassDeclaration.BaseTypeOriginalDefinition = GetBaseTypOriginalDefinition(classSymbol);
                    do
                    {
                        ClassDeclaration.BaseList.Add(baseTypeSymbol.ToString());
                    } while ((baseTypeSymbol = baseTypeSymbol.BaseType) != null);
                }

                if (classSymbol.AllInterfaces != null)
                {
                    ClassDeclaration.BaseList.AddRange(classSymbol.AllInterfaces.Select(x => x.ToString())?.ToList());
                }
            }
        }

        private void Set(ClassDeclaration ClassDeclaration, INamedTypeSymbol classSymbol)
        {
            
        }
    }
}
