using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

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
            InterfaceDeclaration.Identifier = syntaxNode.Identifier.ToString();


            if (interfaceSymbol != null)
            {
                if (interfaceSymbol.BaseType != null)
                {
                    InterfaceDeclaration.BaseType = interfaceSymbol.BaseType.ToString();
                    InterfaceDeclaration.BaseTypeOriginalDefinition = GetBaseTypOriginalDefinition(interfaceSymbol);
                }

                InterfaceDeclaration.Reference.Namespace = GetNamespace(interfaceSymbol);
                InterfaceDeclaration.Reference.Assembly = GetAssembly(interfaceSymbol);
                InterfaceDeclaration.Reference.Version = GetAssemblyVersion(interfaceSymbol);
                InterfaceDeclaration.Reference.AssemblySymbol = interfaceSymbol.ContainingAssembly;
                InterfaceDeclaration.FullIdentifier = string.Concat(InterfaceDeclaration.Reference.Namespace, ".", InterfaceDeclaration.Identifier);

                if (interfaceSymbol.Interfaces != null)
                {
                    InterfaceDeclaration.BaseList = interfaceSymbol.Interfaces.Select(x => x.ToString())?.ToList();
                }
            }

        }
    }
}
