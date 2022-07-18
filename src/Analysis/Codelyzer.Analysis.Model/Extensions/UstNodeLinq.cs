namespace Codelyzer.Analysis.Model
{
    public static class UstNodeLinq
    {
        public static UstList<Annotation> AllAnnotations(this UstNode node)
        {
            return GetNodes<Annotation>(node);
        }

        public static UstList<AttributeList> AllAttributeLists(this UstNode node)
        {
            return GetNodes<AttributeList>(node);
        }

        public static UstList<AttributeArgument> AllAttributeArguments(this UstNode node)
        {
            return GetNodes<AttributeArgument>(node);
        }

        public static UstList<BlockStatement> AllBlockStatements(this UstNode node)
        {
            return GetNodes<BlockStatement>(node);
        }

        public static UstList<ClassDeclaration> AllClasses(this UstNode node)
        {
            return GetNodes<ClassDeclaration>(node);
        }

        public static UstList<ClassBlock> AllClassBlocks(this UstNode node)
        {
            return GetNodes<ClassBlock>(node);
        }

        public static UstList<InterfaceDeclaration> AllInterfaces(this UstNode node)
        {
            return GetNodes<InterfaceDeclaration>(node);
        }

        public static UstList<InterfaceBlock> AllInterfaceBlocks(this UstNode node)
        {
            return GetNodes<InterfaceBlock>(node);
        }

        public static UstList<ExpressionStatement> AllExpressions(this UstNode node)
        {
            return GetNodes<ExpressionStatement>(node);
        }

        public static UstList<InvocationExpression> AllInvocationExpressions(this UstNode node)
        {
            return  GetNodes<InvocationExpression>(node);
        }
        
        public static UstList<ObjectCreationExpression> AllObjectCreationExpressions(this UstNode node)
        {
            return GetNodes<ObjectCreationExpression>(node);
        }

        public static UstList<LiteralExpression> AllLiterals(this UstNode node)
        {
            return GetNodes<LiteralExpression>(node);
        }

        public static UstList<MethodDeclaration> AllMethods(this UstNode node)
        {
            return GetNodes<MethodDeclaration>(node);
        }

        public static UstList<MethodBlock> AllMethodBlocks(this UstNode node)
        {
            return GetNodes<MethodBlock>(node);
        }

        public static UstList<ReturnStatement> AllReturnStatements(this UstNode node)
        {
            return GetNodes<ReturnStatement>(node);
        }

        public static UstList<ConstructorDeclaration> AllConstructors(this UstNode node)
        {
            return GetNodes<ConstructorDeclaration>(node);
        }

        public static UstList<ConstructorBlock> AllConstructorBlocks(this UstNode node)
        {
            return GetNodes<ConstructorBlock>(node);
        }

        public static UstList<NamespaceDeclaration> AllNamespaces(this UstNode node)
        {
            return GetNodes<NamespaceDeclaration>(node);
        }

        public static UstList<NamespaceBlock> AllNamespaceBlocks(this UstNode node)
        {
            return GetNodes<NamespaceBlock>(node);
        }

        public static UstList<UsingDirective> AllUsingDirectives(this UstNode node)
        {
            return GetNodes<UsingDirective>(node);
        }

        public static UstList<ImportsStatement> AllImportsStatements(this UstNode node)
        {
            return GetNodes<ImportsStatement>(node);
        }

        public static UstList<DeclarationNode> AllDeclarationNodes(this UstNode node)
        {
            return GetNodes<DeclarationNode>(node);
        }

        public static UstList<EnumDeclaration> AllEnumDeclarations(this UstNode node)
        {
            return GetNodes<EnumDeclaration>(node);
        }

        public static UstList<StructDeclaration> AllStructDeclarations(this UstNode node)
        {
            return GetNodes<StructDeclaration>(node);
        }

        public static UstList<ArrowExpressionClause> AllArrowExpressionClauses(this UstNode node)
        {
            return GetNodes<ArrowExpressionClause>(node);
        }

        public static UstList<SimpleLambdaExpression> AllSimpleLambdaExpressions(this UstNode node)
        {
            return GetNodes<SimpleLambdaExpression>(node);
        }

        public static UstList<ParenthesizedLambdaExpression> AllParenthesizedLambdaExpressions(this UstNode node)
        {
            return GetNodes<ParenthesizedLambdaExpression>(node);
        }

        public static UstList<LambdaExpression> AllLambdaExpressions(this UstNode node)
        {
            return GetNodes<LambdaExpression>(node);
        }

        public static UstList<Argument> AllArguments(this UstNode node)
        {
            return GetNodes<Argument>(node);
        }

        public static UstList<ArgumentList> AllArgumentLists(this UstNode node)
        {
            return GetNodes<ArgumentList>(node);
        }


        public static UstList<ElementAccess> AllElementAccessExpressions(this UstNode node)
        {
            return GetNodes<ElementAccess>(node);
        }
        public static UstList<MemberAccess> AllMemberAccessExpressions(this UstNode node)
        {
            return GetNodes<MemberAccess>(node);
        }

        public static UstList<EnumBlock> AllEnumBlocks(this UstNode node)
        {
            return GetNodes<EnumBlock>(node);
        }

        private static UstList<T> GetNodes<T>(UstNode node) where T : UstNode
        {
            UstList<T> nodes = new UstList<T>();

            foreach (UstNode child in node.Children)
            {
                if (child != null)
                {
                    if (child is T)
                    {
                        nodes.Add((T)child);
                    }
                    nodes.AddRange(GetNodes<T>(child));
                }
            }

            return nodes;
        }
    }
}
