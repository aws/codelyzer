using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class EnumDeclaration : UstNode
    {
        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public EnumDeclaration()
            : base(IdConstants.EnumIdName)
        {
            Reference = new Reference();
        }
    }
}
