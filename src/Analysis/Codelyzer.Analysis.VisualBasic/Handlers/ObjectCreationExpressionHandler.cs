using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
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
                    if (argumentSyntax.GetExpression() != null)
                        parameter.Name = argumentSyntax.GetExpression().ToString();

                    parameter.SemanticType =
                        SemanticHelper.GetSemanticType(argumentSyntax.GetExpression(), SemanticModel, OriginalSemanticModel);
#pragma warning disable CS0618 // Type or member is obsolete
                    if (Model.Parameters != null)
                    {
                        Model.Parameters.Add(parameter);
                    }
#pragma warning restore CS0618 // Type or member is obsolete

                    var argument = new Argument
                    {
                        Identifier = argumentSyntax.GetExpression().ToString(),
                        SemanticType = SemanticHelper.GetSemanticType(argumentSyntax.GetExpression(), SemanticModel, OriginalSemanticModel)
                    };
                    Model.Arguments.Add(argument);
                }
            }

            IMethodSymbol invokedSymbol = (IMethodSymbol)(SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel)
                ?? SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel)
                ?? SemanticHelper.GetDeclaredOriginalSymbol(syntaxNode, SemanticModel, OriginalSemanticModel));
            ;

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
