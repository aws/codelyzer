using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public abstract class LambdaExpression : UstNode
    {
        [JsonProperty("lambda-type", Order = 1)]
        public abstract string LambdaType { get; }

        [JsonProperty("return-type", Order = 20)]
        public string ReturnType { get; set; }

        [JsonProperty("semantic-properties", Order = 30)]
        public UstList<string> SemanticProperties { get; set; }

        public LambdaExpression(string idName)
            : base(idName)
        {
            SemanticProperties = new UstList<string>();
        }
    }
}
