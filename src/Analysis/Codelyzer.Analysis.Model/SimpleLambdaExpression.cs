using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class SimpleLambdaExpression : LambdaExpression
    {
        [JsonProperty("lambda-type", Order = 2)]
        public override string LambdaType => IdConstants.SimpleLambdaExpressionIdName;

        [JsonProperty("parameter", Order = 10)]
        public Parameter Parameter { get; set; }

        public SimpleLambdaExpression()
            : base(IdConstants.LambdaExpressionIdName)
        {
        }
    }
}
