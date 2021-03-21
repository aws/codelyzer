using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
 
namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class ConstructorDeclarationHandler : UstNodeHandler
    {
        private ConstructorDeclaration Model { get => (ConstructorDeclaration)UstNode; }

        public ConstructorDeclarationHandler(CodeContext context,
            ConstructorDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new ConstructorDeclaration())
        {
            Model.Identifier = syntaxNode.Identifier.ToString();
            SetMetaData(syntaxNode);
        }

        private void SetMetaData(ConstructorDeclarationSyntax syntaxNode)
        {
            foreach (var parameter in syntaxNode.ParameterList.Parameters)
            {
                var param = new Parameter
                {
                    Name = parameter.Identifier.Text
                };

                if (parameter.Type != null)
                    param.Type = parameter.Type.ToString();

                param.SemanticType = SemanticHelper.GetSemanticType(parameter.Type, SemanticModel, OriginalSemanticModel);
                Model.Parameters.Add(param);
            }

            Model.Modifiers = syntaxNode.Modifiers.ToString();

            var methodSymbol = (IMethodSymbol)
                (SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel)
                ?? SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel));
            if (methodSymbol == null) return;
            SemanticHelper.AddMethodProperties(methodSymbol, Model.SemanticProperties);

            Model.SemanticSignature = SemanticHelper.GetSemanticMethodSignature(SemanticModel, syntaxNode);
        }
    }
}