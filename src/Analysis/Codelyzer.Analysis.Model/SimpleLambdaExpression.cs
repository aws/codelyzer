using Newtonsoft.Json;
using System;

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
        public override bool Equals(object obj)
        {
            if (obj is SimpleLambdaExpression)
            {
                return Equals(obj as SimpleLambdaExpression);
            }
            return false;
        }

        public bool Equals(SimpleLambdaExpression compareNode)
        {
            return
                compareNode != null &&
                Parameter?.Equals(compareNode.Parameter) != false &&
                base.Equals(compareNode);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Parameter, base.GetHashCode());
        }
    }
}
