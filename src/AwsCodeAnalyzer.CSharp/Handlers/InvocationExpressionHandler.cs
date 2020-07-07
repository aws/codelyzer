using System;
using System.Collections.Generic;
using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwsCodeAnalyzer.CSharp.Handlers
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
                var mae = ((MemberAccessExpressionSyntax) syntaxNode.Expression);
                Model.MethodName = mae.Name.ToString();
                Model.CallerIdentifier = mae.Expression.ToString();
            }
            else
            {
                // Local invocations
                Model.MethodName = syntaxNode.Expression.ToString();
            }

            foreach (var argumentSyntax in syntaxNode.ArgumentList.Arguments)
            {
                Parameter parameter = new Parameter();
                parameter.Name = argumentSyntax.Expression.ToString();
                parameter.SemanticType = 
                    SemanticHelper.GetSemanticType(argumentSyntax.Expression, SemanticModel);
            }
            
            if (SemanticModel == null) return;
            
            IMethodSymbol invokedSymbol = 
                ((IMethodSymbol)SemanticModel.GetSymbolInfo(syntaxNode).Symbol);
            if (invokedSymbol == null) return;
            
            //Set semantic details
            Model.MethodName = invokedSymbol.Name;
            Model.SemanticNamespace = invokedSymbol.ContainingNamespace.ToString();
            Model.SemanticMethodSignature = invokedSymbol.ToString();
            Model.SemanticOriginalDefinition = invokedSymbol.OriginalDefinition.ToString();
            Model.SemanticReturnType = invokedSymbol.ReturnType.Name;
            
            //Set method properties
            SemanticHelper.AddMethodProperties(invokedSymbol, Model.SemanticProperties);

        }
    }
}