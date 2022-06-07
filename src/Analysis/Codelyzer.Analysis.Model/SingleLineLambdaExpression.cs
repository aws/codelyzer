using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Codelyzer.Analysis.Model
{
    public class SingleLineLambdaExpression : LambdaExpression
    {
        [JsonProperty("lambda-type", Order = 2)]
        public override string LambdaType => IdConstants.SingleLineLambdaExpressionIdName;

        [JsonProperty("parameters", Order = 10)]
        public List<Parameter> Parameters { get; set; }

        public SingleLineLambdaExpression()
            : base(IdConstants.LambdaExpressionIdName)
        {
            Parameters = new List<Parameter>();
        }
        public override bool Equals(object obj)
        {
            if (obj is SingleLineLambdaExpression)
            {
                return Equals(obj as SingleLineLambdaExpression);
            }
            return false;
        }

        public bool Equals(SingleLineLambdaExpression compareNode)
        {
            return
                compareNode != null &&
                Parameters?.SequenceEqual(compareNode.Parameters) != false &&
                base.Equals(compareNode);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Parameters, base.GetHashCode());
        }
    }
}
