using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE;

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
                Model.CallerIdentifier = mae.Expression?.ToString() ?? "";
            }
            else
            {
                // Local invocations
                if (syntaxNode.Expression != null)
                    Model.MethodName = syntaxNode.Expression.ToString();
            }

            if (syntaxNode.ArgumentList != null)
            {
                foreach (var argumentSyntax in syntaxNode.ArgumentList.Arguments)
                {
                    var identifier = "";
                    var semanticType = "";

                    if (argumentSyntax is not OmittedArgumentSyntax)
                    {
                        identifier = argumentSyntax.GetExpression().ToString();
                        semanticType = SemanticHelper.GetSemanticType(argumentSyntax.GetExpression(), SemanticModel);
                    }

                    var parameter = new Parameter()
                    {
                        Name = identifier,
                        SemanticType = semanticType
                    };
    #pragma warning disable CS0618 // Type or member is obsolete
                    Model.Parameters.Add(parameter);
    #pragma warning restore CS0618 // Type or member is obsolete

                    var argument = new Argument
                    {
                        Identifier = identifier,
                        SemanticType = semanticType
                    };
                    Model.Arguments.Add(argument);
                }
            }

            if (SemanticModel == null) return;

            var symbol = SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);
            if (symbol is IPropertySymbol)
            {
                HandlePropertySymbol((IPropertySymbol)symbol);
                return;
            }
            
            var invokedSymbol = (IMethodSymbol)symbol;
            if (invokedSymbol == null) return;
            //Set semantic details
            HandleMethodSymbol(invokedSymbol);
        }

        private void HandleMethodSymbol(IMethodSymbol invokedSymbol)
        {
            Model.MethodName = invokedSymbol.Name;
            if (invokedSymbol.ContainingNamespace != null)
                Model.SemanticNamespace = invokedSymbol.ContainingNamespace.ToString();

            Model.SemanticMethodSignature = invokedSymbol.ToString();

            if (invokedSymbol.ReturnType != null)
                Model.SemanticReturnType = invokedSymbol.ReturnType.Name;

            if (invokedSymbol.ContainingType != null)
            {
                string classNameWithNamespace = invokedSymbol.ContainingType.ToString();
                Model.SemanticClassType = Model.SemanticNamespace == null ? classNameWithNamespace : SemanticHelper.GetSemanticClassType(classNameWithNamespace, Model.SemanticNamespace);
            }

            string originalDefinition = "";
            if (invokedSymbol.IsExtensionMethod && invokedSymbol.ReceiverType != null)
            {
                originalDefinition = invokedSymbol.ReceiverType.ToString();
            }
            else if (invokedSymbol.ContainingType != null)
            {
                originalDefinition = invokedSymbol.ContainingType.ToString();
            }

            Model.SemanticOriginalDefinition =
                $"{originalDefinition}.{Model.MethodName}({string.Join(", ", invokedSymbol.Parameters.Select(p => p.Type))})";


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

        void HandlePropertySymbol(IPropertySymbol invokedSymbol)
        {
            //Set semantic details
            Model.MethodName = invokedSymbol.Name;
            if (invokedSymbol.ContainingNamespace != null)
                Model.SemanticNamespace = invokedSymbol.ContainingNamespace.ToString();

            Model.SemanticMethodSignature = invokedSymbol.ToString();

            if (invokedSymbol.Type != null)
                Model.SemanticReturnType = invokedSymbol.Type.Name;

            if (invokedSymbol.ContainingType != null)
            {
                string classNameWithNamespace = invokedSymbol.ContainingType.ToString();
                Model.SemanticClassType = Model.SemanticNamespace == null ? classNameWithNamespace :
                    SemanticHelper.GetSemanticClassType(classNameWithNamespace, Model.SemanticNamespace);
            }

            string originalDefinition = "";
            if (invokedSymbol.ContainingType != null)
            {
                originalDefinition = invokedSymbol.ContainingType.ToString();
            }
            Model.SemanticOriginalDefinition =
                $"{originalDefinition}.{Model.MethodName}({string.Join(", ", invokedSymbol.Parameters.Select(p => p.Type))})";


            Model.Reference.Namespace = GetNamespace(invokedSymbol);
            Model.Reference.Assembly = GetAssembly(invokedSymbol);
            Model.Reference.Version = GetAssemblyVersion(invokedSymbol);
            Model.Reference.AssemblySymbol = invokedSymbol.ContainingAssembly;

            //Set method properties
            SemanticHelper.AddPropertyProperties(invokedSymbol, Model.SemanticProperties);
        }
    }
}
