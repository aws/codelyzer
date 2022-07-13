using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class MultiLineIfBlock : UstNode
    {

        [JsonProperty("modifiers", Order = 20)]
        public string Modifiers { get; set; }

        public MultiLineIfBlock()
            : base(IdConstants.MultiLineIfBlockName)
        {

        }
    }
}