using System.Collections.Generic;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class InterfaceDeclarationHandler : UstNodeHandler
    {
        // Key: interface full identifier, value: InterfaceDeclaration.
        private static Dictionary<string, InterfaceDeclaration> _interfaceDelcarationCache = new();
        private InterfaceDeclaration InterfaceDeclaration { get => (InterfaceDeclaration)UstNode; }

        public InterfaceDeclarationHandler(CodeContext context,
            InterfaceDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new InterfaceDeclaration())
        {
            var interfaceSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);
            if (interfaceSymbol != null)
            {
                var fullIdentifier = interfaceSymbol.OriginalDefinition.ToString();
                if (_interfaceDelcarationCache.ContainsKey(fullIdentifier))
                {
                    UstNode = _interfaceDelcarationCache[fullIdentifier];
                    return;
                }
                Set(InterfaceDeclaration, interfaceSymbol);
            }
        }
        private void Set(InterfaceDeclaration interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
        {
            var fullIdentifier = interfaceSymbol.OriginalDefinition.ToString();
            if (_interfaceDelcarationCache.ContainsKey(fullIdentifier))
            {

            }
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
            interfaceDeclaration.FullIdentifier = interfaceSymbol.OriginalDefinition.ToString();

            if (interfaceSymbol.Interfaces != null)
            {
                interfaceDeclaration.BaseTypeDeclarationList = new();
                foreach( var ifs in interfaceSymbol.Interfaces)
                {
                    interfaceDeclaration.BaseTypeDeclarationList.Add(GetBaseTypeDeclaration(ifs));
                }
            }
        }

        private InterfaceDeclaration GetBaseTypeDeclaration(INamedTypeSymbol baseTypeSymbol)
        {
            if (baseTypeSymbol == null) return null;

            var fullIdentifier = baseTypeSymbol.OriginalDefinition.ToString();
            if (_interfaceDelcarationCache.ContainsKey(fullIdentifier))
            {
                return _interfaceDelcarationCache[fullIdentifier];
            }

            InterfaceDeclaration baseDeclaration = new();
            Set(baseDeclaration, baseTypeSymbol);

            return baseDeclaration;
        }
    }
}
