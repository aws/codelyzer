namespace Codelyzer.Analysis.Model
{
    public static class ModelFactory
    {
        public static UstNode GetObject(string type)
        {
            UstNode ustNode = null;

            switch (type)
            {
                case IdConstants.RootIdName:
                    ustNode = new RootUstNode();
                    break;

                case IdConstants.UsingDirectiveIdName:
                    ustNode = new UsingDirective();
                    break;

                case IdConstants.NamespaceIdName:
                    ustNode = new NamespaceDeclaration();
                    break;

                case IdConstants.ClassIdName:
                    ustNode = new ClassDeclaration();
                    break;

                case IdConstants.InterfaceIdName:
                    ustNode = new InterfaceDeclaration();
                    break;

                case IdConstants.BodyIdName:
                    ustNode = new BlockStatement();
                    break;

                case IdConstants.ObjectCreationIdName:
                    ustNode = new ObjectCreationExpression();
                    break;

                case IdConstants.InvocationIdName:
                    ustNode = new InvocationExpression();
                    break;

                case IdConstants.LiteralIdName:
                    ustNode = new LiteralExpression();
                    break;

                case IdConstants.MethodIdName:
                    ustNode = new MethodDeclaration();
                    break;
                default: break;
            }

            return ustNode;
        }
    }
}
