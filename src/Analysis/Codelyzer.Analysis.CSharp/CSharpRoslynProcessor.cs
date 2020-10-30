using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.CSharp.Handlers;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Codelyzer.Analysis.CSharp
{
    /// <summary>
    /// Processor that traverses the Syntax tree nodes
    /// </summary>
    public class CSharpRoslynProcessor : CSharpSyntaxVisitor<UstNode>, IDisposable
    {
        private CodeContext context;
        protected SemanticModel SemanticModel { get => context.SemanticModel; }
        protected SyntaxTree SyntaxTree { get => context.SyntaxTree; }
        protected ILogger Logger { get => context.Logger; }
        protected MetaDataSettings MetaDataSettings { get => context.AnalyzerConfiguration.MetaDataSettings; }
        protected RootUstNode RootNode { get; set; }

        public CSharpRoslynProcessor(CodeContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Start traversing the syntax tree
        /// </summary>
        /// <param name="node">The node to start the traversal from</param>
        /// <returns></returns>
        [return: MaybeNull]
        public override UstNode Visit(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }
            if (RootNode == null)
            {
                RootNode = new RootUstNode();
            }

            var children = new List<UstNode>();
            foreach (SyntaxNode child in node.ChildNodes())
            {
                var result = HandleGenericVisit(child);
                if (result != null)
                {
                    children.Add(result);
                }
            }

            RootNode.SetPaths(context.SourceFilePath, SyntaxTree.FilePath);
            RootNode.Language = node.Language;

            RootNode.Children.AddRange(children);
            
            return RootNode;
        }

        private List<UstNode> HandleGenericMembers(in SyntaxList<SyntaxNode> nodeMembers)
        {
            List<UstNode> members = new List<UstNode>();

            foreach (var nodeMember in nodeMembers)
            {
                var member = HandleGenericVisit(nodeMember);
                if (null != member)
                {
                    members.Add(member);
                }
            }

            return members;
        }
        private UstNode HandleGenericVisit(SyntaxNode node)
        {
            try
            {
                return base.Visit(node);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, node.ToString());
                return null;
            }
        }

        [return: MaybeNull]
        public override UstNode DefaultVisit(SyntaxNode node)
        {
            return null;
        }

        /* ---- Overrides ----------------------*/
        public override UstNode VisitUsingDirective(UsingDirectiveSyntax node)
        {
            UsingDirectiveHandler handler = new UsingDirectiveHandler(context, node);
            return handler.UstNode;
        }

        public override UstNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            NamespaceDeclarationHandler handler = new NamespaceDeclarationHandler(context, node);
            handler.UstNode.Children.AddRange(HandleGenericMembers(node.Members));
            return handler.UstNode;
        }

        public override UstNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            ClassDeclarationHandler handler = new ClassDeclarationHandler(context, node);
            HandleReferences(((ClassDeclaration)handler.UstNode).Reference );
            handler.UstNode.Children.AddRange(HandleGenericMembers(node.Members));

            var attributes = node.DescendantNodes().OfType<AttributeSyntax>();
            foreach (var attribute in attributes)
            {
                handler.UstNode.Children.Add(VisitAttribute((AttributeSyntax)attribute));
            }

            var identifierNames = node.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var identifierName in identifierNames)
            {
                var identifier = VisitIdentifierName((IdentifierNameSyntax)identifierName);
                if (identifier != null)
                {
                    handler.UstNode.Children.Add(identifier);
                }
            }

            return handler.UstNode;
        }

        public override UstNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            ConstructorDeclarationHandler handler = new ConstructorDeclarationHandler(context, node);
            handler.UstNode.Children.Add(VisitBlock(node.Body));

            var identifierNames = node.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var identifierName in identifierNames)
            {
                var identifier = VisitIdentifierName((IdentifierNameSyntax)identifierName);
                if (identifier != null)
                {
                    handler.UstNode.Children.Add(identifier);
                }
            }

            return handler.UstNode;
        }

        public override UstNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            MethodDeclarationHandler handler = new MethodDeclarationHandler(context, node);
            //For abstract methods, it will not have any body
            if (node.Body != null)
            {
                handler.UstNode.Children.Add(VisitBlock(node.Body));
            }

            var identifierNames = node.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var identifierName in identifierNames)
            {
                var identifier = VisitIdentifierName((IdentifierNameSyntax)identifierName);
                if (identifier != null)
                {
                    handler.UstNode.Children.Add(identifier);
                }
            }

            return handler.UstNode;
        }

        public override UstNode VisitBlock(BlockSyntax node)
        {
            BlockStatementHandler handler = new BlockStatementHandler(context, node);
            var result = handler.UstNode;

            if (MetaDataSettings.MethodInvocations)
            {
                var expressions = node.DescendantNodes().OfType<InvocationExpressionSyntax>();
                foreach (var expression in expressions)
                {
                    result.Children.Add(VisitInvocationExpression((InvocationExpressionSyntax)expression));
                }
            }

            var objCreations = node.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
            foreach (var expression in objCreations)
            {
                result.Children.Add(VisitObjectCreationExpression((ObjectCreationExpressionSyntax)expression));
            }

            if (MetaDataSettings.LiteralExpressions)
            {
                var literalExpressions = node.DescendantNodes().OfType<LiteralExpressionSyntax>();
                foreach (var expression in literalExpressions)
                {
                    result.Children.Add(VisitLiteralExpression((LiteralExpressionSyntax)expression));
                }
            }

            var identifierNames = node.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var identifierName in identifierNames)
            {
                var identifier = VisitIdentifierName((IdentifierNameSyntax)identifierName);
                if (identifier != null)
                {
                    result.Children.Add(identifier);
                }
            }

            return result;
        }

        public override UstNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            return base.VisitExpressionStatement(node);
        }

        public override UstNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (!MetaDataSettings.LiteralExpressions) return null;

            LiteralExpressionHandler handler = new LiteralExpressionHandler(context, node);
            return handler.UstNode;
        }

        public override UstNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (!MetaDataSettings.MethodInvocations) return null;

            InvocationExpressionHandler handler = new InvocationExpressionHandler(context, node);
            HandleReferences(((InvocationExpression)handler.UstNode).Reference);
            return handler.UstNode;
        }

        public override UstNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            ObjectCreationExpressionHandler handler = new ObjectCreationExpressionHandler(context, node);

            var identifierNames = node.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var identifierName in identifierNames)
            {
                var identifier = VisitIdentifierName((IdentifierNameSyntax)identifierName);
                if (identifier != null)
                {
                    handler.UstNode.Children.Add(identifier);
                }
            }
            return handler.UstNode;
        }

        public override UstNode VisitAttribute(AttributeSyntax node)
        {
            if (MetaDataSettings.Annotations)
            {
                AttributeHandler handler = new AttributeHandler(context, node);
                HandleReferences(((Annotation)handler.UstNode).Reference);
                return handler.UstNode;
            }
            return null;
        }

        public override UstNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (MetaDataSettings.DeclarationNodes)
            {
                IdentifierNameHandler handler = new IdentifierNameHandler(context, node);
                if (!string.IsNullOrEmpty(handler.UstNode.Identifier))
                {
                    HandleReferences(((DeclarationNode)handler.UstNode).Reference);
                    return handler.UstNode;
                }
            }
            return null;
        }
        
        private void HandleReferences(Reference reference)
        {
            if (MetaDataSettings.ReferenceData
                && !RootNode.References.Contains(reference))
            {
                var rootReference = new Reference() { Assembly = reference.Assembly, Namespace = reference.Namespace };
                if (reference.AssemblySymbol != null)
                {
                    var metaDataReference = SemanticModel.Compilation.GetMetadataReference(reference.AssemblySymbol);
                    if (metaDataReference != null)
                    {
                        rootReference.AssemblyLocation = metaDataReference.Display;
                    }
                }
                RootNode.References.Add(rootReference);
            }
        }

        public void Dispose()
        {
            context?.Dispose();
            RootNode = null;
        }

    }

}
