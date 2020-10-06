using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class DeclarationNode : UstNode
    {
        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public DeclarationNode()
            : base(IdConstants.DeclarationNodeIdName)
        {
            Reference = new Reference();
        }
    }
}