namespace AwsCodeAnalyzer.Model
{
    public class ExpressionStatement : UstNode
    {
        public ExpressionStatement(NodeType type)
            : base(type.Id, type.Name)
        {
        }
    }
}