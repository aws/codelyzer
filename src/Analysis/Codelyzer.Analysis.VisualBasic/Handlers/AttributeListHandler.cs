using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class AttributeListHandler : UstNodeHandler
    {
        private AttributeList Model { get => (AttributeList)UstNode; }

        public AttributeListHandler(CodeContext context,
            AttributeListSyntax syntaxNode)
            : base(context, syntaxNode, new AttributeList())
        {
            Model.Identifier = syntaxNode.Attributes.ToString();

            var symbol = SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);

            if (symbol != null && symbol.ContainingNamespace != null)
            {
                Model.Reference.Namespace = GetNamespace(symbol);
                Model.Reference.Assembly = GetAssembly(symbol);
                Model.Reference.Version = GetAssemblyVersion(symbol);
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
