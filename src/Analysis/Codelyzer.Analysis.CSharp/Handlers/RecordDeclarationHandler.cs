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
                if(recordSymbol.BaseType != null)
                {
                    RecordDeclaration.BaseType = recordSymbol.BaseType.ToString();               
                    RecordDeclaration.BaseTypeOriginalDefinition = GetBaseTypOriginalDefinition(recordSymbol);
                    RecordDeclaration.Reference.Namespace = GetNamespace(recordSymbol);
                    RecordDeclaration.Reference.Assembly = GetAssembly(recordSymbol);
                    RecordDeclaration.Reference.Version = GetAssemblyVersion(recordSymbol);
                    RecordDeclaration.Reference.AssemblySymbol = recordSymbol.ContainingAssembly;
                    RecordDeclaration.FullIdentifier = recordSymbol.OriginalDefinition.ToString();
                }
                
                if(recordSymbol.Interfaces != null)
                {
                    RecordDeclaration.BaseList = recordSymbol.Interfaces.Select(x => x.ToString())?.ToList();
                }
            }
        }
    }
}
