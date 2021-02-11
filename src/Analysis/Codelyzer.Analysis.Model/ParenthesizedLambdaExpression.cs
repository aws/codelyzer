using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class ParenthesizedLambdaExpression : LambdaExpression
    {
        [JsonProperty("lambda-type", Order = 1)]
        public override string LambdaType => IdConstants.ParenthesizedLambdaExpressionIdName;

        [JsonProperty("parameters", Order = 10)]
        public List<Parameter> Parameters { get; set; }

        public ParenthesizedLambdaExpression()
            : base(IdConstants.LambdaExpressionIdName)
        {
            Parameters = new List<Parameter>();
        }
    }
}
