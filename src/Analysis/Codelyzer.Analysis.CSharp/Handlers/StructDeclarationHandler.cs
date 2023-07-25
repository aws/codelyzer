using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class StructDeclarationHandler : UstNodeHandler
    {
        private StructDeclaration StructDeclaration { get => (StructDeclaration)UstNode; }

        public StructDeclarationHandler(CodeContext context,
            StructDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new StructDeclaration())
        {
            var structSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);
            StructDeclaration.Identifier = syntaxNode.Identifier.ToString();

            if (structSymbol != null)
            {
                StructDeclaration.Reference.Namespace = GetNamespace(structSymbol);
                StructDeclaration.Reference.Assembly = GetAssembly(structSymbol);
                StructDeclaration.Reference.Version = GetAssemblyVersion(structSymbol);
                StructDeclaration.Reference.AssemblySymbol = structSymbol.ContainingAssembly;
                StructDeclaration.FullIdentifier = structSymbol.OriginalDefinition.ToString();

                StructDeclaration.BaseList = new System.Collections.Generic.List<string>();
                if (structSymbol.BaseType != null)
                {
                    var baseTypeSymbol = structSymbol.BaseType;
                    StructDeclaration.BaseType = baseTypeSymbol.ToString();
                    StructDeclaration.BaseTypeOriginalDefinition = GetBaseTypOriginalDefinition(structSymbol);
                    do
                    {
                        StructDeclaration.BaseList.Add(baseTypeSymbol.ToString());
                    } while ((baseTypeSymbol = baseTypeSymbol.BaseType) != null);
                }

                if (structSymbol.AllInterfaces != null)
                {
                    StructDeclaration.BaseList.AddRange(structSymbol.AllInterfaces.Select(x => x.ToString())?.ToList());
                }
            }
        }
    }
}
