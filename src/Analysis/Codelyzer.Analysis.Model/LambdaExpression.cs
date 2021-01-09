using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class LambdaExpression : UstNode
    {
        [JsonProperty("return-type", Order = 20)]
        public string ReturnType { get; set; }

        [JsonProperty("semantic-properties", Order = 30)]
        public UstList<string> SemanticProperties { get; set; }

        public LambdaExpression(string idName)
            : base(idName)
        {
        }
    }
}
