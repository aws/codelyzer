using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class ElementAccess : UstNode
    {
        [JsonProperty("expression", Order = 10)]
        public string Expression { get; set; }

        public ElementAccess()
            : base(IdConstants.ElementAccessIdName)
        {
        }

        public ElementAccess(string idName)
            : base(idName)
        {
        }
    }
}
