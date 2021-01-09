using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class SimpleLambdaExpression : LambdaExpression
    {
        [JsonProperty("parameter", Order = 10)]
        public Parameter Parameter { get; set; }

        public SimpleLambdaExpression()
            : base(IdConstants.SimpleLambdaExpressionIdName)
        {
        }
    }
}
