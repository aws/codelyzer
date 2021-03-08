using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Codelyzer.Analysis.CSharp
{
    /// <summary>
    /// Helper class for getting semantic info
    /// </summary>
    public static class SemanticHelper
    {
        /// <summary>
        /// Gets name of type from TypeSyntax
        /// </summary>
        /// <param name="typeSyntax">The TypeSyntax parameter to get info about</param>
        /// <param name="semanticModel">An instance of the semantic model</param>
        /// <returns>Name of the type</returns>
        public static string GetSemanticType(TypeSyntax typeSyntax, 
            SemanticModel semanticModel,
            SemanticModel preportSemanticModel = null)
        {
            if (semanticModel == null && preportSemanticModel == null) return null;
            
            string type = null;

            var typeInfo = semanticModel.GetTypeInfo(typeSyntax);
            if(typeInfo.Type == null && preportSemanticModel != null)
            {
                typeInfo = preportSemanticModel.GetTypeInfo(typeSyntax);
            }

            if (typeInfo.Type != null)
            {
                type = typeInfo.Type.Name;
            }

            return type;
        }

        /// <summary>
        /// Gets name of type from ExpressionSyntax
        /// </summary>
        /// <param name="expressionSyntax">The ExpressionSyntax to get info about</param>
        /// <param name="semanticModel">An instance of the semantic model</param>
        /// <returns>Name of the type</returns>
        public static string GetSemanticType(ExpressionSyntax expressionSyntax, 
            SemanticModel semanticModel,
            SemanticModel preportSemanticModel = null)
        {
            if (semanticModel == null && preportSemanticModel == null) return null;
            
            string type = null;

            var typeInfo = semanticModel.GetTypeInfo(expressionSyntax);
            if (typeInfo.Type == null && preportSemanticModel != null)
            {
                typeInfo = preportSemanticModel.GetTypeInfo(expressionSyntax);
            }

            if (typeInfo.Type != null)
            {
                type = typeInfo.Type.Name;
            }

            return type;
        }

        public static ISymbol GetSemanticSymbol(SyntaxNode syntaxNode,
            SemanticModel semanticModel,
            SemanticModel preportSemanticModel = null)
        {
            if (semanticModel == null && preportSemanticModel == null) return null;

            var symbol = semanticModel.GetSymbolInfo(syntaxNode).Symbol;
            if (symbol == null && preportSemanticModel != null)
            {
                symbol = preportSemanticModel.GetSymbolInfo(syntaxNode).Symbol;
            }

            return symbol;
        }

        public static INamedTypeSymbol GetDeclaredSymbol(SyntaxNode syntaxNode,
            SemanticModel semanticModel,
            SemanticModel preportSemanticModel = null)
        {
            if (semanticModel == null && preportSemanticModel == null) return null;

            var symbol = semanticModel.GetDeclaredSymbol(syntaxNode) as INamedTypeSymbol;
            if (symbol == null && preportSemanticModel != null)
            {
                symbol = preportSemanticModel.GetDeclaredSymbol(syntaxNode) as INamedTypeSymbol;
            }

            return symbol;
        }

        /// <summary>
        /// Gets name of type from IdentifierNameSyntax
        /// </summary>
        /// <param name="identifierNameSyntax">The IdentifierNameSyntax to get info about</param>
        /// <param name="semanticModel">An instance of the semantic model</param>
        /// <returns>Name of the type</returns>
        public static string GetSemanticType(IdentifierNameSyntax identifierNameSyntax,
          SemanticModel semanticModel,
          SemanticModel preportSemanticModel)
        {
            if (semanticModel == null && preportSemanticModel == null) return null;

            string type = null;
            
            var typeInfo = semanticModel.GetTypeInfo(identifierNameSyntax); 
            if (typeInfo.Type == null && preportSemanticModel != null)
            {
                typeInfo = preportSemanticModel.GetTypeInfo(identifierNameSyntax);
            }

            if (typeInfo.Type != null)
            {
                type = typeInfo.Type.Name;
            }

            return type;
        }

        /// <summary>
        /// Populates a List with all the method properties
        /// </summary>
        /// <param name="invokedSymbol">The method to analyze</param>
        /// <param name="properties">The List to populate</param>
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
