namespace AwsCodeAnalyzer.Model
{
    public class BlockStatement : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.BodyId, 
            IdConstants.BodyIdName);
        public BlockStatement()
            : base(TYPE.Name)
        {
        }
    }
}