using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class AttributeHandler : UstNodeHandler
    {
        private Annotation Model { get => (Annotation)UstNode; }

        public AttributeHandler(CodeContext context,
            AttributeSyntax syntaxNode)
            : base(context, syntaxNode, new Annotation())
        {
            Model.Identifier = syntaxNode.Name.ToString();

            var symbolInfo = SemanticModel.GetSymbolInfo(syntaxNode);
            if (symbolInfo.Symbol != null && symbolInfo.Symbol.ContainingNamespace != null)
            {
                var symbol = symbolInfo.Symbol;

                Model.Reference.Namespace = GetNamespace(symbol);
                Model.Reference.Assembly = GetAssembly(symbol);
                Model.Reference.AssemblySymbol = symbol.ContainingAssembly;

                if (symbol.ContainingType != null)
                {
                    string classNameWithNamespace = symbol.ContainingType.ToString();
                    Model.SemanticClassType = Model.Reference.Namespace == null ? classNameWithNamespace :
                        SemanticHelper.GetSemanticClassType(classNameWithNamespace, Model.Reference.Namespace);
                }
            }
        }

    }
}
