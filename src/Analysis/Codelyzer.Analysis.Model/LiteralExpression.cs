using Newtonsoft.Json;
using System;

namespace Codelyzer.Analysis.Model
{
    public class LiteralExpression : ExpressionStatement
    {        
        [JsonProperty("literal-type")]
        public string LiteralType { get; set; }
        
        [JsonProperty("semantic-type")]
        public string SemanticType { get; set; }
        public LiteralExpression()
            : base(IdConstants.LiteralIdName)
        {
        }
        public override bool Equals(object obj)
        {
            if (obj is LiteralExpression)
            {
                return Equals(obj as LiteralExpression);
            }
            return false;
        }

        public bool Equals(LiteralExpression compareNode)
        {
            return
                compareNode != null &&
                LiteralType?.Equals(compareNode.LiteralType) != false &&
                SemanticType?.Equals(compareNode.SemanticType) != false &&
                base.Equals(compareNode);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(LiteralType, SemanticType, base.GetHashCode());
        }
    }
}
