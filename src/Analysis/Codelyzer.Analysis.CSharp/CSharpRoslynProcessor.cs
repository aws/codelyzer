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
        private readonly CodeContext _context;
        protected SemanticModel SemanticModel { get => _context.SemanticModel; }
        protected SyntaxTree SyntaxTree { get => _context.SyntaxTree; }
        protected ILogger Logger { get => _context.Logger; }
        protected MetaDataSettings MetaDataSettings { get => _context.AnalyzerConfiguration.MetaDataSettings; }
        protected RootUstNode RootNode { get; set; }

        public CSharpRoslynProcessor(CodeContext context)
        {
            _context = context;
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

            RootNode.SetPaths(_context.SourceFilePath, SyntaxTree.FilePath);
            RootNode.Language = node.Language;

            RootNode.Children.AddRange(children);
            AssignParentNode(RootNode.Children, RootNode);

            return RootNode;
        }

        [return: MaybeNull]
        public override UstNode DefaultVisit(SyntaxNode node)
        {
            return null;
        }

        /* ---- Overrides ----------------------*/
        public override UstNode VisitUsingDirective(UsingDirectiveSyntax node)
        {
            UsingDirectiveHandler handler = new UsingDirectiveHandler(_context, node);
            return handler.UstNode;
        }
        public override UstNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            NamespaceDeclarationHandler handler = new NamespaceDeclarationHandler(_context, node);
            return handler.UstNode;
        }
        public override UstNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            ClassDeclarationHandler handler = new ClassDeclarationHandler(_context, node);
            HandleReferences(((ClassDeclaration)handler.UstNode).Reference);
            return handler.UstNode;
        }
        public override UstNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            if (!MetaDataSettings.InterfaceDeclarations) return null;

            InterfaceDeclarationHandler handler = new InterfaceDeclarationHandler(_context, node);
            HandleReferences(((InterfaceDeclaration)handler.UstNode).Reference);

            return handler.UstNode;
        }
        public override UstNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            ConstructorDeclarationHandler handler = new ConstructorDeclarationHandler(_context, node);
            return handler.UstNode;
        }
        public override UstNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            MethodDeclarationHandler handler = new MethodDeclarationHandler(_context, node);
            return handler.UstNode;
        }
        public override UstNode VisitReturnStatement(ReturnStatementSyntax node)
        {
            ReturnStatementHandler handler = new ReturnStatementHandler(_context, node);
            return handler.UstNode;
        }
        public override UstNode VisitBlock(BlockSyntax node)
        {
            BlockStatementHandler handler = new BlockStatementHandler(_context, node);
            if (!string.IsNullOrEmpty(handler.UstNode.Identifier))
            {
                return handler.UstNode;
            }
            else
            {
                return null;
            }
        }
        public override UstNode VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            ArrowExpressionClauseHandler handler = new ArrowExpressionClauseHandler(_context, node);
            return handler.UstNode;
        }
        public override UstNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            return base.VisitExpressionStatement(node);
        }
        public override UstNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (!MetaDataSettings.LiteralExpressions) return null;

            LiteralExpressionHandler handler = new LiteralExpressionHandler(_context, node);
            return handler.UstNode;
        }
        public override UstNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (!MetaDataSettings.MethodInvocations) return null;

            InvocationExpressionHandler handler = new InvocationExpressionHandler(_context, node);
            HandleReferences(((InvocationExpression)handler.UstNode).Reference);
            return handler.UstNode;
        }
        public override UstNode VisitArgument(ArgumentSyntax node)
        {
            if (!MetaDataSettings.InvocationArguments) return null;

            ArgumentHandler handler = new ArgumentHandler(_context, node);
            return handler.UstNode;
        }
        public override UstNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            ObjectCreationExpressionHandler handler = new ObjectCreationExpressionHandler(_context, node);
            return handler.UstNode;
        }
        public override UstNode VisitAttribute(AttributeSyntax node)
        {
            if (!MetaDataSettings.Annotations) return null;

            AttributeHandler handler = new AttributeHandler(_context, node);
            HandleReferences(((Annotation)handler.UstNode).Reference);
            return handler.UstNode;
        }
        public override UstNode VisitAttributeArgument(AttributeArgumentSyntax node)
        {
            if (!MetaDataSettings.Annotations) return null;

            AttributeArgumentHandler handler = new AttributeArgumentHandler(_context, node);
            return handler.UstNode;
        }
        public override UstNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (MetaDataSettings.DeclarationNodes)
            {
                IdentifierNameHandler handler = new IdentifierNameHandler(_context, node);
                if (!string.IsNullOrEmpty(handler.UstNode.Identifier))
                {
                    HandleReferences(((DeclarationNode)handler.UstNode).Reference);
                    return handler.UstNode;
                }
            }
            return null;
        }
        public override UstNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            if (MetaDataSettings.EnumDeclarations)
            {
                EnumDeclarationHandler handler = new EnumDeclarationHandler(_context, node);
                if (!string.IsNullOrEmpty(handler.UstNode.Identifier))
                {
                    HandleReferences(((EnumDeclaration)handler.UstNode).Reference);
                    return handler.UstNode;
                }
            }
            return null;
        }
        public override UstNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (!MetaDataSettings.StructDeclarations) return null;
            
            StructDeclarationHandler handler = new StructDeclarationHandler(_context, node);
            if (!string.IsNullOrEmpty(handler.UstNode.Identifier))
            {
                HandleReferences(((StructDeclaration)handler.UstNode).Reference);
            }

            return handler.UstNode;
        }

        public override UstNode VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            if (!MetaDataSettings.ElementAccess) return null;

            var handler = new ElementAccessExpressionHandler(_context, node);
            HandleReferences(((ElementAccess)handler.UstNode).Reference);
            return handler.UstNode;
        }

        public override UstNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (!MetaDataSettings.MemberAccess) return null;

            var handler = new MemberAccessExpressionHandler(_context, node);
            return handler.UstNode;
        }

        public override UstNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            if (!MetaDataSettings.LambdaMethods) return null;

            var handler = new SimpleLambdaExpressionHandler(_context, node);
            return handler.UstNode;
        }

        public override UstNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            if (!MetaDataSettings.LambdaMethods) return null;

            var handler = new ParenthesizedLambdaExpressionHandler(_context, node);
            return handler.UstNode;
        }

        private void HandleReferences(in Reference reference)
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
        private List<UstNode> HandleGenericMembers(List<SyntaxNode> children)
        {
            List<UstNode> childUstNodes = new List<UstNode>();

            foreach (var child in children)
            {
                var childUstNode = HandleGenericVisit(child);
                if (childUstNode != null)
                {
                    childUstNodes.Add(childUstNode);
                }
                //If we're not handling the node, we'll collapse the level into the parent so that we can still visit the children
                else
                {
                    var grandChildren = child.ChildNodes();
                    if (grandChildren.Any())
                    {
                        var grandChildUstNodes = HandleGenericMembers(grandChildren.ToList());
                        if (grandChildUstNodes.Any())
                        {
                            childUstNodes.AddRange(grandChildUstNodes);
                        }
                    }
                }
            }

            return childUstNodes;
        }
        private UstNode HandleGenericVisit(SyntaxNode node)
        {
            try
            {
                var ustNode = base.Visit(node);
                if (ustNode != null)
                {
                    AddChildNodes(ustNode.Children, node);
                    AssignParentNode(ustNode.Children, ustNode);
                }
                return ustNode;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, node.ToString());
                return null;
            }
        }
        private void AddChildNodes(UstList<UstNode> nodeChildren, SyntaxNode syntaxNode)
        {
            var children = HandleGenericMembers(syntaxNode.ChildNodes()?.ToList());
            if (children != null && nodeChildren != null)
            {
                nodeChildren.AddRange(children);
            }
        }
        private void AssignParentNode(List<UstNode> children, UstNode parentNode)
        {
            foreach (var child in children)
            {
                child.Parent = parentNode;
            }
        }
        public void Dispose()
        {
            _context?.Dispose();
            RootNode = null;
        }
    }
}
