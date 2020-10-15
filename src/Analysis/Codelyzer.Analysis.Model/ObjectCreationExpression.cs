namespace Codelyzer.Analysis.Model
{
    public class ObjectCreationExpression : InvocationExpression
    {        
        public ObjectCreationExpression() : base(IdConstants.ObjectCreationIdName)
        {
        }
    }
}
