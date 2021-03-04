using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class AttributeArgument : UstNode
    {
        [JsonProperty("argument-name", Order = 20)]
        public string ArgumentName { get; set; }

        [JsonProperty("argument-expression", Order = 25)]
        public string ArgumentExpression { get; set; }

        public AttributeArgument()
            : base(IdConstants.AttributeArgumentIdName)
        {
        }
    }
}
