using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class MemberAccessExpressionHandler : UstNodeHandler
    {
        private MemberAccess Model { get => (MemberAccess)UstNode; }

        public MemberAccessExpressionHandler(CodeContext context,
            MemberAccessExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new MemberAccess())
        {
            Model.Identifier = syntaxNode.ToString();
            Model.Name = syntaxNode.Name?.ToString();
            Model.Expression = syntaxNode.Expression?.ToString();

            var invokedSymbol = SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);

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
