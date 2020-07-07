using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.CSharp.Handlers;
using AwsCodeAnalyzer.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;
using Serilog.Core;

namespace AwsCodeAnalyzer.CSharp
{
    public class CSharpRoslynProcessor : CSharpSyntaxVisitor<UstNode>
    {
        private CodeContext context;
        protected SemanticModel SemanticModel { get => context.SemanticModel; }
        protected SyntaxTree SyntaxTree { get => context.SyntaxTree; }
        protected ILogger Logger { get => context.Logger; }

        protected RootUstNode RootNode { get; set; }

        public CSharpRoslynProcessor(CodeContext context)
        {
            this.context = context;
        }
        
        [return: MaybeNull]
        public override UstNode Visit(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
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

            if (RootNode == null)
            {
                RootNode = new RootUstNode();
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
                Logger.Error(ex, node.ToString());
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
            handler.UstNode.Children.AddRange(HandleGenericMembers(node.Members));

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

            return handler.UstNode;
        }

        public override UstNode VisitBlock(BlockSyntax node)
        {
            BlockStatementHandler handler = new BlockStatementHandler(context, node);
            var result = handler.UstNode;
            var expressions = node.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var expression in expressions)
            {
                result.Children.Add(VisitInvocationExpression((InvocationExpressionSyntax)expression));
            }
            
            var literalExpressions = node.DescendantNodes().OfType<LiteralExpressionSyntax>();
            foreach (var expression in literalExpressions)
            {
                result.Children.Add(VisitLiteralExpression((LiteralExpressionSyntax)expression));
            }
            return result;
        }

        public override UstNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            return base.VisitExpressionStatement(node);
        }

        public override UstNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            LiteralExpressionHandler handler = new LiteralExpressionHandler(context, node);
            return handler.UstNode;
        }

        public override UstNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            InvocationExpressionHandler handler = new InvocationExpressionHandler(context, node);
            return handler.UstNode;
        }
    }

}