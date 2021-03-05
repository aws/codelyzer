using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class AssignmentExpression : UstNode
    {

        [JsonProperty("left", Order = 20)]
        public string Left { get; set; }

        [JsonProperty("right", Order = 25)]
        public string Right { get; set; }

        [JsonProperty("operator", Order = 30)]
        public string Operator { get; set; }

        public AssignmentExpression()
            : base(IdConstants.AssignmentExpressionIdName)
        {
        }

        public AssignmentExpression(string idName)
            : base(idName)
        {
        }
    }
}
