using Newtonsoft.Json;
using System;

namespace Codelyzer.Analysis.Model
{
    public class BinaryExpression : ExpressionStatement
    {
        [JsonProperty("semantic-type")]
        public string SemanticType { get; set; }
        public BinaryExpression()
            : base(IdConstants.BinaryExpressionName)
        {
        }
        public override bool Equals(object obj)
        {
            if (obj is BinaryExpression)
            {
                return Equals(obj as BinaryExpression);
            }
            return false;
        }

        public bool Equals(BinaryExpression compareNode)
        {
            return
                compareNode != null &&
                SemanticType?.Equals(compareNode.SemanticType) != false &&
                base.Equals(compareNode);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(SemanticType, base.GetHashCode());
        }
    }
}
