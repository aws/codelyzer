using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class ParenthesizedLambdaExpression : LambdaExpression
    {
        [JsonProperty("lambda-type", Order = 2)]
        public override string LambdaType => IdConstants.ParenthesizedLambdaExpressionIdName;

        [JsonProperty("parameters", Order = 10)]
        public List<Parameter> Parameters { get; set; }

        public ParenthesizedLambdaExpression()
            : base(IdConstants.LambdaExpressionIdName)
        {
            Parameters = new List<Parameter>();
        }
        public override bool Equals(object obj)
        {
            if (obj is ParenthesizedLambdaExpression)
            {
                return Equals(obj as ParenthesizedLambdaExpression);
            }
            return false;
        }

        public bool Equals(ParenthesizedLambdaExpression compareNode)
        {
            return
                compareNode != null &&
                Parameters?.SequenceEqual(compareNode.Parameters) != false &&
                base.Equals(compareNode);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Parameters,base.GetHashCode());
        }
    }
}
