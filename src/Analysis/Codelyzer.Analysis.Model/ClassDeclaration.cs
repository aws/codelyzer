using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class ClassDeclaration : UstNode
    {
        [JsonProperty("base-type", Order = 10)]
        public string BaseType { get; set; }

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
