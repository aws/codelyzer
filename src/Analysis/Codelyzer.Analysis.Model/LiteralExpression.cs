using Newtonsoft.Json;

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
    }
}
