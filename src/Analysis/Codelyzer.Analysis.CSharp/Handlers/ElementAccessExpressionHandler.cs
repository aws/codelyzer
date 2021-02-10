using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class ElementAccessExpressionHandler : UstNodeHandler
    {
        private ElementAccess Model { get => (ElementAccess)UstNode; }

        public ElementAccessExpressionHandler(CodeContext context,
            ElementAccessExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new ElementAccess())
        {
            Model.Identifier = syntaxNode.ToString();
            Model.Expression = syntaxNode.Expression?.ToString();

            var invokedSymbol = SemanticModel.GetSymbolInfo(syntaxNode).Symbol;

            if (invokedSymbol != null)
            {
                Model.Reference.Namespace = GetNamespace(invokedSymbol);
                Model.Reference.Assembly = GetAssembly(invokedSymbol);
                Model.Reference.AssemblySymbol = invokedSymbol.ContainingAssembly;

                if (invokedSymbol.ContainingType != null)
                {
                    string classNameWithNamespace = invokedSymbol.ContainingType.ToString();
                    Model.SemanticClassType = Model.Reference.Namespace == null ? classNameWithNamespace :
                        SemanticHelper.GetSemanticClassType(classNameWithNamespace, Model.Reference.Namespace);
                }
            }
        }
    }
}
