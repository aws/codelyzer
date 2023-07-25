using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class RecordDeclarationHandler : UstNodeHandler
    {
        private RecordDeclaration RecordDeclaration { get => (RecordDeclaration)UstNode; }

        public RecordDeclarationHandler(CodeContext context,
            RecordDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new RecordDeclaration())
        {
            var recordSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);

            RecordDeclaration.Identifier = syntaxNode.Identifier.ToString();
            RecordDeclaration.Modifiers = syntaxNode.Modifiers.ToString();

            if (recordSymbol != null)
            {
                RecordDeclaration.FullIdentifier = recordSymbol.OriginalDefinition.ToString();
                RecordDeclaration.Reference.Namespace = GetNamespace(recordSymbol);
                RecordDeclaration.Reference.Assembly = GetAssembly(recordSymbol);
                RecordDeclaration.Reference.Version = GetAssemblyVersion(recordSymbol);
                RecordDeclaration.Reference.AssemblySymbol = recordSymbol.ContainingAssembly;

                RecordDeclaration.BaseList = new System.Collections.Generic.List<string>();
                if (recordSymbol.BaseType != null)
                {
                    var baseTypeSymbol = recordSymbol.BaseType;
                    RecordDeclaration.BaseType = baseTypeSymbol.ToString();
                    RecordDeclaration.BaseTypeOriginalDefinition = GetBaseTypOriginalDefinition(recordSymbol);
                    do
                    {
                        RecordDeclaration.BaseList.Add(baseTypeSymbol.ToString());
                    } while ((baseTypeSymbol = baseTypeSymbol.BaseType) != null);
                }

                if (recordSymbol.AllInterfaces != null)
                {
                    RecordDeclaration.BaseList.AddRange(recordSymbol.AllInterfaces.Select(x => x.ToString())?.ToList());
                }
            }
        }
    }
}
