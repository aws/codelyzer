using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class ClassDeclaration : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.ClassId, 
            IdConstants.ClassIdName);

        [JsonProperty("base-type", Order = 10)]
        public string BaseType { get; set; }
        [JsonProperty("base-namespace", Order = 11)]
        public string SemanticNamespace { get; set; }
        [JsonProperty("base-assembly", Order = 12)]
        public string SemanticAssembly { get; set; }
        public ClassDeclaration()
            : base(TYPE.Name)
        {
        }
    }
}