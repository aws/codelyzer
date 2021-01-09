using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class ParenthesizedLambdaExpression : LambdaExpression
    {
        [JsonProperty("parameters", Order = 10)]
        public List<Parameter> Parameters { get; set; }

        public ParenthesizedLambdaExpression()
            : base(IdConstants.ParenthesizedLambdaExpressionIdName)
        {
            Parameters = new List<Parameter>();
        }
    }
}
