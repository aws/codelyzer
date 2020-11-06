using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class InterfaceDeclaration : UstNode
    {
        [JsonProperty("base-type", Order = 10)]
        public string BaseType { get; set; }

        [JsonProperty("base-type-original-def", Order = 11)]
        public string BaseTypeOriginalDefinition { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public string SemanticAssembly { get; set; }
        public InterfaceDeclaration()
            : base(IdConstants.InterfaceIdName)
        {
            Reference = new Reference();
        }
    }
}
