using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class LiteralExpression : ExpressionStatement
    {
        public static readonly NodeType Type = new NodeType(IdConstants.LiteralId, 
            IdConstants.LiteralIdName);
        
        [JsonProperty("literal-type")]
        public string LiteralType { get; set; }
        
        [JsonProperty("semantic-type")]
        public string SemanticType { get; set; }
        public LiteralExpression()
            : base(Type)
        {
        }
    }
}