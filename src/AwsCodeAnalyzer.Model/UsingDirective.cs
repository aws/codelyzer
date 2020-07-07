namespace AwsCodeAnalyzer.Model
{
    public class UsingDirective : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.UsingDirectiveId, 
            IdConstants.UsingDirectiveIdName);
        public UsingDirective()
            : base(TYPE)
        {
        }
    }
}