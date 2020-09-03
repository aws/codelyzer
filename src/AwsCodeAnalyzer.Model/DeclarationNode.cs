using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class DeclarationNode : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.DeclarationNodeId,
            IdConstants.DeclarationNodeIdName);

        [JsonProperty("semantic-namespace")]
        public string SemanticNamespace { get; set; }
        [JsonProperty("semantic-assembly")]
        public string SemanticAssembly { get; set; }
        public DeclarationNode()
            : base(TYPE.Name)
        {
        }
    }
}