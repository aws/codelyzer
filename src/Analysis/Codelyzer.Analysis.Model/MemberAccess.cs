using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class MemberAccess : UstNode
    {
        [JsonProperty("name", Order = 10)]
        public string Name { get; set; }

        [JsonProperty("expression", Order = 11)]
        public string Expression { get; set; }

        public MemberAccess()
            : base(IdConstants.MemberAccessIdName)
        {
        }

        public MemberAccess(string idName)
            : base(idName)
        {
        }
    }
}
