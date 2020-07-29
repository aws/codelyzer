namespace AwsCodeAnalyzer.Model
{
    public class NamespaceDeclaration : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.NamespaceId, 
                                                    IdConstants.NamespaceIdName);
        public NamespaceDeclaration()
            : base(TYPE.Name)
        {
        }
    }
}