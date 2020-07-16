namespace AwsCodeAnalyzer.Model
{
    public class ObjectCreationExpression : InvocationExpression
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.ObjectCreationId, 
            IdConstants.ObjectCreationIdName);
        
        public ObjectCreationExpression() : base(TYPE)
        {
        }
    }
}