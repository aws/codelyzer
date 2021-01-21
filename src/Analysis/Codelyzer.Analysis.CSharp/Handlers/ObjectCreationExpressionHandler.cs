using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codelyzer.Analysis.CSharp.Handlers
{
    public class ObjectCreationExpressionHandler : UstNodeHandler
    {
        private ObjectCreationExpression Model { get => (ObjectCreationExpression) UstNode; }

        public ObjectCreationExpressionHandler(CodeContext context, 
            ObjectCreationExpressionSyntax syntaxNode) 
            : base(context, syntaxNode, new ObjectCreationExpression())
        {
            Model.Identifier = syntaxNode.ToString();
            SetMetaData(syntaxNode);
        }

        private void SetMetaData(ObjectCreationExpressionSyntax syntaxNode)
        {
            Model.MethodName = syntaxNode.Type.ToString();

            if (syntaxNode.ArgumentList != null)
            {
                foreach (var argumentSyntax in syntaxNode.ArgumentList.Arguments)
                {
                    Parameter parameter = new Parameter();
                    if (argumentSyntax.Expression != null)
                        parameter.Name = argumentSyntax.Expression.ToString();

                    parameter.SemanticType =
                        SemanticHelper.GetSemanticType(argumentSyntax.Expression, SemanticModel);
                    if (Model.Parameters != null)
                    {
                        Model.Parameters.Add(parameter);
                    }

                    var argument = new Argument
                    {
                        Identifier = argumentSyntax.Expression.ToString(),
                        SemanticType = SemanticHelper.GetSemanticType(argumentSyntax.Expression, SemanticModel)
                    };
                    Model.Arguments.Add(argument);
                }
            }

            if (SemanticModel == null) return;
            
            IMethodSymbol invokedSymbol = 
                ((IMethodSymbol)SemanticModel.GetSymbolInfo(syntaxNode).Symbol);
            if (invokedSymbol == null) return;
            
            //Set semantic details
            Model.MethodName = invokedSymbol.Name;
            if (invokedSymbol.ContainingNamespace != null)
                Model.SemanticNamespace = invokedSymbol.ContainingNamespace.ToString();
            
            Model.SemanticMethodSignature = invokedSymbol.ToString();
            if (invokedSymbol.OriginalDefinition != null)
                Model.SemanticOriginalDefinition = invokedSymbol.OriginalDefinition.ToString();
            
            if (invokedSymbol.ReturnType != null)
                Model.SemanticReturnType = invokedSymbol.ReturnType.Name;
            
            if (invokedSymbol.ContainingType != null)
            {
                string classNameWithNamespace = invokedSymbol.ContainingType.ToString();
                Model.SemanticClassType = Model.SemanticNamespace == null ? classNameWithNamespace : 
                    SemanticHelper.GetSemanticClassType(classNameWithNamespace, Model.SemanticNamespace);
            }
            
            //Set method properties
            SemanticHelper.AddMethodProperties(invokedSymbol, Model.SemanticProperties);
        }
    }
}
