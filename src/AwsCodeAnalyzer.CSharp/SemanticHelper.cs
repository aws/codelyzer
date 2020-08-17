using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwsCodeAnalyzer.CSharp
{
    public static class SemanticHelper
    {
        public static string GetSemanticType(TypeSyntax parameter, 
            SemanticModel semanticModel)
        {
            if (semanticModel == null) return null;
            
            string type = null;

            var typeInfo = semanticModel.GetTypeInfo(parameter);
            if (typeInfo.Type != null)
            {
                type = semanticModel.GetTypeInfo(parameter).Type.Name;
            }

            return type;
        }
        
        public static string GetSemanticType(ExpressionSyntax expressionSyntax, 
            SemanticModel semanticModel)
        {
            if (semanticModel == null) return null;
            
            string type = null;

            var typeInfo = semanticModel.GetTypeInfo(expressionSyntax);
            if (typeInfo.Type != null)
            {
                type = semanticModel.GetTypeInfo(expressionSyntax).Type.Name;
            }

            return type;
        }

        public static string GetSemanticType(IdentifierNameSyntax identifierNameSyntax,
          SemanticModel semanticModel)
        {
            if (semanticModel == null) return null;

            string type = null;

            var typeInfo = semanticModel.GetTypeInfo(identifierNameSyntax);
            if (typeInfo.Type != null)
            {
                type = semanticModel.GetTypeInfo(identifierNameSyntax).Type.Name;
            }

            return type;
        }

        public static void AddMethodProperties(IMethodSymbol invokedSymbol, List<string> properties)
        {
            //Set method properties
            properties.Add(invokedSymbol.DeclaredAccessibility.ToString());
            if (invokedSymbol.IsAsync) properties.Add("async");
            if (invokedSymbol.IsOverride) properties.Add("override");
            if (invokedSymbol.IsAbstract) properties.Add("abstract");
            if (invokedSymbol.IsExtern) properties.Add("extern");
            if (invokedSymbol.IsSealed) properties.Add("sealed");
            if (invokedSymbol.IsStatic) properties.Add("static");
            if (invokedSymbol.IsVirtual) properties.Add("virtual");
            if (invokedSymbol.IsReadOnly) properties.Add("readonly");
        }

        public static string GetSemanticClassType(string classNameWithNamespace, string semanticNamespace)
        {
            Match match = Regex.Match(classNameWithNamespace, String.Format("{0}.(.*)", Regex.Escape(semanticNamespace)));
            return match.Success ? match.Groups[1].Value : classNameWithNamespace;
        }
        
    }
}