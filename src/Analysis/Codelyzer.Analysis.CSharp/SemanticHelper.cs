using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Codelyzer.Analysis.CSharp
{
    /// <summary>
    /// Helper class for getting semantic info
    /// </summary>
    public static class SemanticHelper
    {
        /// <summary>
        /// Gets SymbolInfo for a syntax node
        /// </summary>
        /// <param name="syntaxNode">The node to get symbol info for</param>
        /// <param name="semanticModel">An instance of the semantic model</param>
        /// <returns>SymbolInfo of a node</returns>
        public static SymbolInfo? GetSymbolInfo(SyntaxNode syntaxNode,
            SemanticModel semanticModel)
        {
            return semanticModel?.GetSymbolInfo(syntaxNode);
        }

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
            if (typeInfo.Type == null && preportSemanticModel != null)
            {
                try
                {
                    typeInfo = preportSemanticModel.GetTypeInfo(typeSyntax);
                }
                catch (Exception)
                {
                    //When looking for a symbol, and the semantic model is not passed, this generates an error.
                    //We don't log this error because this is an expected behavior when there's no previous semantic models
                }
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
                try
                {
                    typeInfo = preportSemanticModel.GetTypeInfo(expressionSyntax);
                }
                catch (Exception)
                {
                    //When looking for a symbol, and the semantic model is not passed, this generates an error.
                    //We don't log this error because this is an expected behavior when there's no previous semantic models
                }
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

            var symbol = semanticModel?.GetSymbolInfo(syntaxNode).Symbol;
            if (symbol == null && preportSemanticModel != null)
            {
                try
                {
                    symbol = preportSemanticModel.GetSymbolInfo(syntaxNode).Symbol;
                }
                catch (Exception)
                {
                    //When looking for a symbol, and the semantic model is not passed, this generates an error.
                    //We don't log this error because this is an expected behavior when there's no previous semantic models
                }
            }

            try
            {
                var symbolInfo = semanticModel?.GetSymbolInfo(syntaxNode);
                if (symbol == null && symbolInfo.Value.CandidateSymbols.Length > 0)
                {
                    symbol = symbolInfo.Value.CandidateSymbols[0];
                }
            }
            catch (Exception)
            {
                // We're trying to get candidate symbols, and we might not find any so we're ok with this erroring out
            }
            return symbol;
        }

        public static INamedTypeSymbol GetDeclaredSymbol(SyntaxNode syntaxNode,
            SemanticModel semanticModel,
            SemanticModel preportSemanticModel = null)
        {
            if (semanticModel == null && preportSemanticModel == null) return null;

            var symbol = semanticModel?.GetDeclaredSymbol(syntaxNode) as INamedTypeSymbol;
            if (symbol == null && preportSemanticModel != null)
            {
                try
                {
                    symbol = preportSemanticModel.GetDeclaredSymbol(syntaxNode) as INamedTypeSymbol;
                }
                catch (Exception)
                {
                    //When looking for a symbol, and the semantic model is not passed, this generates an error.
                    //We don't log this error because this is an expected behavior when there's no previous semantic models
                }
            }

            return symbol;
        }

        public static ISymbol GetDeclaredOriginalSymbol(SyntaxNode syntaxNode,
            SemanticModel semanticModel,
            SemanticModel preportSemanticModel = null)
        {
            if (semanticModel == null && preportSemanticModel == null) return null;

            var symbol = semanticModel?.GetDeclaredSymbol(syntaxNode);
            if (symbol == null && preportSemanticModel != null)
            {
                try
                {
                    symbol = preportSemanticModel.GetDeclaredSymbol(syntaxNode);
                }
                catch (Exception)
                {
                    //When looking for a symbol, and the semantic model is not passed, this generates an error.
                    //We don't log this error because this is an expected behavior when there's no previous semantic models
                }
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
                try
                {
                    typeInfo = preportSemanticModel.GetTypeInfo(identifierNameSyntax);
                }
                catch (Exception)
                {
                    //When looking for a symbol, and the semantic model is not passed, this generates an error.
                    //We don't log this error because this is an expected behavior when there's no previous semantic models
                }
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

        /// <summary>
        /// Returns the semantic method signature of a method declaration (fully qualified method names and parameter types)
        /// </summary>
        /// <param name="semanticModel">Semantic model of syntax tree containing the method declaration</param>
        /// <param name="syntaxNode">Method declaration node</param>
        /// <returns>The semantic method signature</returns>
        public static string GetSemanticMethodSignature(SemanticModel semanticModel, SemanticModel originalSemanticModel, BaseMethodDeclarationSyntax syntaxNode)
        {
            var semanticMethodNameAndParameters = GetDeclaredOriginalSymbol(syntaxNode, semanticModel, originalSemanticModel)?.ToString();
            var joinedModifiers = string.Join(" ", syntaxNode.Modifiers.Select(m => m.ToString()));

            return $"{joinedModifiers} {semanticMethodNameAndParameters}".Trim();
        }

    }
}
