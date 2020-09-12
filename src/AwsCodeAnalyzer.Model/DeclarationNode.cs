using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class DeclarationNode : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.DeclarationNodeId,
            IdConstants.DeclarationNodeIdName);

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public DeclarationNode()
            : base(TYPE.Name)
        {
            Reference = new Reference();
        }
    }
}