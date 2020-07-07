namespace AwsCodeAnalyzer.Model
{
    public class ClassDeclaration : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.ClassId, 
            IdConstants.ClassIdName);
        public ClassDeclaration()
            : base(TYPE)
        {
        }
    }
}