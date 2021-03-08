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

            var symbol = SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);

            if (symbol != null && symbol.ContainingNamespace != null)
            {
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
