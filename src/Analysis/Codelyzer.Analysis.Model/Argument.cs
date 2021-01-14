using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class Argument : UstNode
    {
        [JsonProperty("semantic-type", Order = 10)]
        public string SemanticType { get; set; }

        public Argument()
            : base(IdConstants.ArgumentIdName)
        {
        }
    }
}
