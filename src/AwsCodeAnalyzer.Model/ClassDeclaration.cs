using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class ClassDeclaration : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.ClassId, 
            IdConstants.ClassIdName);

        [JsonProperty("base-type", Order = 10)]
        public string BaseType { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public string SemanticAssembly { get; set; }
        public ClassDeclaration()
            : base(TYPE.Name)
        {
            Reference = new Reference();
        }
    }
}