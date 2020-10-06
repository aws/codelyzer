namespace AwsCodeAnalyzer.Model
{
    public class ObjectCreationExpression : InvocationExpression
    {        
        public ObjectCreationExpression() : base(new NodeType(IdConstants.ObjectCreationId,
            IdConstants.ObjectCreationIdName))
        {
        }
    }
}