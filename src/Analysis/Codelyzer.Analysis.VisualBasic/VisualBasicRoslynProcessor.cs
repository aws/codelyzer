using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.VisualBasic.Handlers;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic
{
    /// <summary>
    /// Processor that traverses the Syntax tree nodes
    /// </summary>
    public class VisualBasicRoslynProcessor : VisualBasicSyntaxVisitor<UstNode>, IDisposable
    {
        private readonly CodeContext _context;
        protected SemanticModel SemanticModel { get => _context.SemanticModel; }
        protected SyntaxTree SyntaxTree { get => _context.SyntaxTree; }
        protected ILogger Logger { get => _context.Logger; }
        protected MetaDataSettings MetaDataSettings { get => _context.AnalyzerConfiguration.MetaDataSettings; }
        protected RootUstNode RootNode { get; set; }

        public VisualBasicRoslynProcessor(CodeContext context)
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
        public override UstNode VisitImportsStatement(ImportsStatementSyntax node)
        {
            ImportsStatementHandler handler = new ImportsStatementHandler(_context, node);
            return handler.UstNode;
        }

        public override UstNode VisitClassBlock(ClassBlockSyntax node)
        {
            ClassBlockHandler handler = new ClassBlockHandler(_context, node);
            return handler.UstNode;
        }

        public override UstNode VisitClassStatement(ClassStatementSyntax node)
        {
            ClassStatementHandler handler = new ClassStatementHandler(_context, node);
            return handler.UstNode;
        }

        public override UstNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            FieldDeclarationHandler handler = new FieldDeclarationHandler(_context, node);
            return handler.UstNode;
        }

        public override UstNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            VariableDeclaratorHandler handler = new VariableDeclaratorHandler(_context, node);
            return handler.UstNode;
        }

        private void HandleReferences(in Reference reference)
        {
            if (MetaDataSettings.ReferenceData
                && !RootNode.References.Contains(reference))
            {
                var rootReference = new Reference() { Assembly = reference.Assembly, Namespace = reference.Namespace, Version = reference.Version };
                if (reference.AssemblySymbol != null)
                {
                    var metaDataReference = SemanticModel?.Compilation.GetMetadataReference(reference.AssemblySymbol);
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
