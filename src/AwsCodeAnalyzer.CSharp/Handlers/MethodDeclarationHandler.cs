using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwsCodeAnalyzer.CSharp.Handlers
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
            Model.ReturnType = syntaxNode.ReturnType.ToString();
            Model.SemanticReturnType = 
                SemanticHelper.GetSemanticType(syntaxNode.ReturnType, SemanticModel);

            foreach (var parameter in syntaxNode.ParameterList.Parameters)
            {
                var param = new Parameter();
                param.Name = parameter.Identifier.Text;
                param.Type = parameter.Type.ToString();
                param.SemanticType = 
                    SemanticHelper.GetSemanticType(parameter.Type, SemanticModel);
                Model.Parameters.Add(param);
            }

            Model.Modifiers = syntaxNode.Modifiers.ToString();
            
           
            var methodSymbol  = (IMethodSymbol)
                (SemanticModel.GetSymbolInfo(syntaxNode).Symbol ?? 
                    SemanticModel.GetDeclaredSymbol(syntaxNode));
            if (methodSymbol == null) return;
            SemanticHelper.AddMethodProperties(methodSymbol, Model.SemanticProperties);
        }
    }
}