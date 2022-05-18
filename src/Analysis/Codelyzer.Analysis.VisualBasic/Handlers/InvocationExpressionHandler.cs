using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class InvocationExpressionHandler : UstNodeHandler
    {
        private InvocationExpression Model { get => (InvocationExpression)UstNode; }

        public InvocationExpressionHandler(CodeContext context,
            InvocationExpressionSyntax syntaxNode)
            : base(context, syntaxNode, new InvocationExpression())
        {
            Model.Identifier = syntaxNode.ToString();
            SetMetaData(syntaxNode);
        }

        private void SetMetaData(InvocationExpressionSyntax syntaxNode)
        {
            // Set method name and expression (classname)
            // context.Logger.Debug(syntaxNode.ToString());
            
            if (syntaxNode.Expression is MemberAccessExpressionSyntax)
            {
                //Object or Class invocations
                var mae = ((MemberAccessExpressionSyntax)syntaxNode.Expression);
                Model.MethodName = mae.Name.ToString();
                Model.CallerIdentifier = mae.Expression.ToString();
            }
            else
            {
                // Local invocations
                if (syntaxNode.Expression != null)
                    Model.MethodName = syntaxNode.Expression.ToString();
            }

            foreach (var argumentSyntax in syntaxNode.ArgumentList.Arguments)
            {
                Parameter parameter = new Parameter();
                if (argumentSyntax.GetExpression() != null)
                    parameter.Name = argumentSyntax.GetExpression().ToString();

                parameter.SemanticType =
                    SemanticHelper.GetSemanticType(argumentSyntax.GetExpression(), SemanticModel);

#pragma warning disable CS0618 // Type or member is obsolete
                Model.Parameters.Add(parameter);
#pragma warning restore CS0618 // Type or member is obsolete

                var argument = new Argument
                {
                    Identifier = argumentSyntax.GetExpression().ToString(),
                    SemanticType = SemanticHelper.GetSemanticType(argumentSyntax.GetExpression(), SemanticModel)
                };

                Model.Arguments.Add(argument);
            }

            if (SemanticModel == null) return;

            IMethodSymbol invokedSymbol = (IMethodSymbol)SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);

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


            Model.Reference.Namespace = GetNamespace(invokedSymbol);
            Model.Reference.Assembly = GetAssembly(invokedSymbol);
            Model.Reference.Version = GetAssemblyVersion(invokedSymbol);
            Model.Reference.AssemblySymbol = invokedSymbol.ContainingAssembly;

            if (invokedSymbol.ReducedFrom != null)
            {
                Model.IsExtension = true;
            }

            //Set method properties
            SemanticHelper.AddMethodProperties(invokedSymbol, Model.SemanticProperties);

        }
    }
}
