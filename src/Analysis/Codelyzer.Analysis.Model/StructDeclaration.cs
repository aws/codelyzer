using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class StructDeclaration : UstNode
    {
        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public StructDeclaration()
            : base(IdConstants.StructIdName)
        {
            Reference = new Reference();
        }
    }
}
