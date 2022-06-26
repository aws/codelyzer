using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class InterfaceDeclarationHandler : UstNodeHandler
    {
        private InterfaceDeclaration InterfaceDeclaration { get => (InterfaceDeclaration)UstNode; }

        public InterfaceDeclarationHandler(CodeContext context,
            InterfaceDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new InterfaceDeclaration())
        {
            var interfaceSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);
            Set(InterfaceDeclaration, interfaceSymbol);
        }
        private void Set(InterfaceDeclaration interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
        {
            if (interfaceSymbol != null)
            {
                var syntaxNodes = interfaceSymbol.DeclaringSyntaxReferences;
                if (syntaxNodes.Length > 0)
                {
                    var syntaxNode = (ClassDeclarationSyntax)syntaxNodes[0].GetSyntax();
                    interfaceDeclaration.Identifier = syntaxNode.Identifier.ToString();
                }
                
                interfaceDeclaration.Reference.Namespace = GetNamespace(interfaceSymbol);
                interfaceDeclaration.Reference.Assembly = GetAssembly(interfaceSymbol);
                interfaceDeclaration.Reference.Version = GetAssemblyVersion(interfaceSymbol);
                interfaceDeclaration.Reference.AssemblySymbol = interfaceSymbol.ContainingAssembly;
                interfaceDeclaration.FullIdentifier = string.Concat(interfaceDeclaration.Reference.Namespace, ".", interfaceDeclaration.Identifier);

                if (interfaceSymbol.Interfaces != null)
                {
                    interfaceDeclaration.BaseTypeDeclarationList = new();
                    foreach( var ifs in interfaceSymbol.Interfaces)
                    {
                        interfaceDeclaration.BaseTypeDeclarationList.Add(GetBaseTypeDeclaration(ifs));
                    }
                }
            }
        }

        private InterfaceDeclaration GetBaseTypeDeclaration(INamedTypeSymbol baseTypeSymbol)
        {
            if (baseTypeSymbol == null) return null;

            InterfaceDeclaration baseDeclaration = new();
            Set(baseDeclaration, baseTypeSymbol);

            return baseDeclaration;
        }
    }
}
