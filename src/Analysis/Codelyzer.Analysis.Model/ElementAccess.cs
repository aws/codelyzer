using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class ElementAccess : UstNode
    {
        [JsonProperty("expression", Order = 10)]
        public string Expression { get; set; }

        [JsonProperty("semantic-class-type", Order = 14)]
        public string SemanticClassType { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }

        public ElementAccess()
            : base(IdConstants.ElementAccessIdName)
        {
            Reference = new Reference();
        }

        public ElementAccess(string idName)
            : base(idName)
        {
            Reference = new Reference();
        }
    }
}
