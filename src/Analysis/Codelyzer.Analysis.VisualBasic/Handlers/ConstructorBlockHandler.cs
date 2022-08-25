using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class ConstructorBlockHandler : UstNodeHandler
    {
        private ConstructorBlock Model { get => (ConstructorBlock)UstNode; }

        public ConstructorBlockHandler(CodeContext context,
            ConstructorBlockSyntax syntaxNode)
            : base(context, syntaxNode, new ConstructorBlock())
        {
            Model.Identifier = syntaxNode.ToString();
            SetMetaData(syntaxNode);
        }

        private void SetMetaData(ConstructorBlockSyntax syntaxNode)
        {
            foreach (var parameter in syntaxNode.SubNewStatement.ParameterList.Parameters)
            {
                var param = new Parameter
                {
                    Name = parameter.Identifier.ToString()
                };
                
                if (parameter.AsClause?.Type != null)
                    param.Type = parameter.AsClause.Type.ToString();

                param.SemanticType = SemanticHelper.GetSemanticType(parameter.AsClause?.Type, SemanticModel, OriginalSemanticModel);
                Model.Parameters.Add(param);
            }

            Model.Modifiers = syntaxNode.SubNewStatement.Modifiers.ToString();

            var methodSymbol = (IMethodSymbol)
                (SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel)
                ?? SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel)
                ?? SemanticHelper.GetDeclaredOriginalSymbol(syntaxNode, SemanticModel, OriginalSemanticModel));
            if (methodSymbol == null) return;
            SemanticHelper.AddMethodProperties(methodSymbol, Model.SemanticProperties);
            Model.SemanticSignature = SemanticHelper.GetSemanticMethodSignature(SemanticModel, OriginalSemanticModel, syntaxNode);

        }
    }
}
