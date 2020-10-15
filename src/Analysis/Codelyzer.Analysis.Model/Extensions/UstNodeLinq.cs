using System.Xml.Serialization;

namespace Codelyzer.Analysis.Model
{
    public static class UstNodeLinq
    {
        public static UstList<BlockStatement> AllBlockStatements(this UstNode node)
        {
            return GetNodes<BlockStatement>(node);
        }

        public static UstList<ClassDeclaration> AllClasses(this UstNode node)
        {
            return GetNodes<ClassDeclaration>(node);
        }

        public static UstList<ExpressionStatement> AllExpressions(this UstNode node)
        {
            return GetNodes<ExpressionStatement>(node);
        }

        public static UstList<InvocationExpression> AllInvocationExpressions(this UstNode node)
        {
            // Combine method invocations and object creations
            var objCreations = GetNodes<ObjectCreationExpression>(node)
                .ConvertAll(x => (InvocationExpression)x);
            
            var result = GetNodes<InvocationExpression>(node);
            result.AddRange(objCreations);
            
            return result;
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

        public static UstList<NamespaceDeclaration> AllNamespaces(this UstNode node)
        {
            return GetNodes<NamespaceDeclaration>(node);
        }

        public static UstList<UsingDirective> AllUsingDirectives(this UstNode node)
        {
            return GetNodes<UsingDirective>(node);
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
                    else
                    {
                        nodes.AddRange(GetNodes<T>(child));
                    }
                }
            }

            return nodes;
        }
        
    }
}
