using AwsCodeAnalyzer.Model;

namespace AwsCodeAnalyzer.Common
{
    public class AnalyzerUtils
    {
        public static UstList<BlockStatement> AllBlockStatements(UstNode node)
        {
            return GetNodes<BlockStatement>(node);
        }

        public static UstList<ClassDeclaration> AllClasses(UstNode node)
        {
            return GetNodes<ClassDeclaration>(node);
        }

        public static UstList<ExpressionStatement> AllExpressions(UstNode node)
        {
            return GetNodes<ExpressionStatement>(node);
        }

        public static UstList<InvocationExpression> AllInvocationExpressions(UstNode node)
        {
            return GetNodes<InvocationExpression>(node);
        }

        public static UstList<LiteralExpression> AllLiterals(UstNode node)
        {
            return GetNodes<LiteralExpression>(node);
        }

        public static UstList<MethodDeclaration> AllMethods(UstNode node)
        {
            return GetNodes<MethodDeclaration>(node);
        }

        public static UstList<NamespaceDeclaration> AllNamespaces(UstNode node)
        {
            return GetNodes<NamespaceDeclaration>(node);
        }

        public static UstList<UsingDirective> AllUsingDirectives(UstNode node)
        {
            return GetNodes<UsingDirective>(node);
        }

        private static UstList<T> GetNodes<T>(UstNode node) where T : UstNode
        {
            UstList<T> nodes = new UstList<T>();

            foreach (UstNode child in node.Children)
            {
                if (child.GetType() == typeof(T))
                {
                    nodes.Add((T) child);
                } else
                {
                    nodes.AddRange(GetNodes<T>(child));
                }
            }

            return nodes;
        }
    }
}
