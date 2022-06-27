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
        private Dictionary<string, InterfaceDeclaration> _interfaceDeclarationCache = new();
        private InterfaceDeclaration InterfaceDeclaration { get => (InterfaceDeclaration)UstNode; }

        public InterfaceDeclarationHandler(CodeContext context,
            InterfaceDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new InterfaceDeclaration())
        {
            var interfaceSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);
            if (interfaceSymbol != null)
            {
                var fullIdentifier = GetFullIdentifier(interfaceSymbol);
                if (_interfaceDeclarationCache.ContainsKey(fullIdentifier))
                {
                    UstNode = _interfaceDeclarationCache[fullIdentifier];
                    return;
                }
                Set(InterfaceDeclaration, interfaceSymbol);
                _interfaceDeclarationCache.Add(fullIdentifier, InterfaceDeclaration);
            }
        }
        private void Set(InterfaceDeclaration interfaceDeclaration, INamedTypeSymbol interfaceSymbol)
        {
            interfaceDeclaration.Identifier = interfaceSymbol.Name;
            interfaceDeclaration.FullIdentifier = GetFullIdentifier(interfaceSymbol);
            interfaceDeclaration.Reference.Namespace = GetNamespace(interfaceSymbol);
            interfaceDeclaration.Reference.Assembly = GetAssembly(interfaceSymbol);
            interfaceDeclaration.Reference.Version = GetAssemblyVersion(interfaceSymbol);
            interfaceDeclaration.Reference.AssemblySymbol = interfaceSymbol.ContainingAssembly;

            if (interfaceSymbol.AllInterfaces != null)
            {
                interfaceDeclaration.AllBaseTypeDeclarationList = new();
                foreach( var ifs in interfaceSymbol.AllInterfaces)
                {
                    interfaceDeclaration.AllBaseTypeDeclarationList.Add(GetBaseTypeDeclaration(ifs));
                }
            }
        }

        private InterfaceDeclaration GetBaseTypeDeclaration(INamedTypeSymbol baseTypeSymbol)
        {
            var fullIdentifier = GetFullIdentifier(baseTypeSymbol);
            if (_interfaceDeclarationCache.ContainsKey(fullIdentifier))
            {
                return _interfaceDeclarationCache[fullIdentifier];
            }

            InterfaceDeclaration baseDeclaration = new();
            Set(baseDeclaration, baseTypeSymbol);
            _interfaceDeclarationCache.Add(fullIdentifier, baseDeclaration);

            return baseDeclaration;
        }
    }
}
