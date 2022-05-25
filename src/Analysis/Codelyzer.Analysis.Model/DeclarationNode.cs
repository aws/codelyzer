using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class DeclarationNode : UstNode
    {
        [JsonProperty("full-identifier", Order = 98)]
        public string FullIdentifier { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public DeclarationNode()
            : base(IdConstants.DeclarationNodeIdName)
        {
            Reference = new Reference();
        }
    }
}
