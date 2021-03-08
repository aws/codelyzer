using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class MethodDeclarationHandler : UstNodeHandler
    {
        private MethodDeclaration Model { get => (MethodDeclaration)UstNode; }

        public MethodDeclarationHandler(CodeContext context, 
            MethodDeclarationSyntax syntaxNode)
            : base(context, syntaxNode, new MethodDeclaration())
        {
            Model.Identifier = syntaxNode.Identifier.ToString();
            SetMetaData(syntaxNode);
        }

        private void SetMetaData(MethodDeclarationSyntax syntaxNode)
        {
            if (syntaxNode.ReturnType != null)
                Model.ReturnType = syntaxNode.ReturnType.ToString();

            Model.SemanticReturnType =
                SemanticHelper.GetSemanticType(syntaxNode.ReturnType, SemanticModel, OriginalSemanticModel);

            if (syntaxNode.ParameterList != null)
            {
                foreach (var parameter in syntaxNode.ParameterList.Parameters)
                {
                    var param = new Parameter();
                    if (parameter.Identifier != null)
                        param.Name = parameter.Identifier.Text;
                    
                    if (parameter.Type != null)
                        param.Type = parameter.Type.ToString();
                    
                    param.SemanticType =
                        SemanticHelper.GetSemanticType(parameter.Type, SemanticModel, OriginalSemanticModel);
                    Model.Parameters.Add(param);
                }
            }

            Model.Modifiers = syntaxNode.Modifiers.ToString();
            
           var methodSymbol = (IMethodSymbol)
                (SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel)
                ?? SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel));
            if (methodSymbol == null) return;
            SemanticHelper.AddMethodProperties(methodSymbol, Model.SemanticProperties);
        }
    }
}
