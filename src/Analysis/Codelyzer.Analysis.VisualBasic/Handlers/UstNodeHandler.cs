﻿using System;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class UstNodeHandler
    {
        protected CodeContext context;
        protected SemanticModel OriginalSemanticModel { get => context.PreportSemanticModel; }
        protected SemanticModel SemanticModel { get => context.SemanticModel; }
        protected SyntaxTree SyntaxTree { get => context.SyntaxTree; }
        public UstNode  UstNode { get; set; }

        public UstNodeHandler(CodeContext context, VisualBasicSyntaxNode syntaxNode, UstNode ustNode)
        {
            this.context = context;
            this.UstNode = ustNode;

            if (syntaxNode == null || syntaxNode.GetLocation() == null) return;

            if (!context.AnalyzerConfiguration.MetaDataSettings.LocationData) return;

            TextSpan textSpan = new TextSpan();
            //internally  it uses 0 based index; 
            textSpan.StartLinePosition = syntaxNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            textSpan.StartCharPosition = syntaxNode.GetLocation().GetLineSpan().StartLinePosition.Character + 1;

            textSpan.EndLinePosition = syntaxNode.GetLocation().GetLineSpan().EndLinePosition.Line + 1;
            textSpan.EndCharPosition = syntaxNode.GetLocation().GetLineSpan().EndLinePosition.Character + 1;

            ustNode.TextSpan = textSpan;

        }

        protected string GetNamespace(ISymbol symbol)
        {
            return symbol.ContainingNamespace != null 
                ? symbol.ContainingNamespace.ToString().Trim() : string.Empty;
        }
        protected string GetAssembly(ISymbol symbol)
        {
            return symbol.ContainingAssembly != null && symbol.ContainingNamespace.Name != null
                ? symbol.ContainingAssembly.Name.ToString().Trim() : string.Empty;
        }

        protected string GetAssemblyVersion(ISymbol symbol)
        {
            return symbol.ContainingAssembly?.Identity?.Version?.ToString();
        }
        protected string GetBaseTypOriginalDefinition(INamedTypeSymbol symbol)
        {
            return symbol.BaseType?.OriginalDefinition.ToString();
        }
    }

}
