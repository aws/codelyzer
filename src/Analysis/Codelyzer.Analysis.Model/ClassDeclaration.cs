using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class ClassDeclaration : UstNode
    {
        [JsonProperty("base-type", Order = 10)]
        public string BaseType { get; set; }

        [JsonProperty("base-type-original-def", Order = 11)]
        public string BaseTypeOriginalDefinition { get; set; }

        [JsonProperty("modifiers", Order = 20)]
        public string Modifiers { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public string SemanticAssembly { get; set; }
        public ClassDeclaration()
            : base(IdConstants.ClassIdName)
        {
            Reference = new Reference();
        }
    }
}
