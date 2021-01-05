using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class ReturnStatement : UstNode
    {
        [JsonProperty("semantic-return-type", Order = 10)]
        public string SemanticReturnType { get; set; }

        public ReturnStatement()
            : base(IdConstants.ReturnStatementIdName)
        {
        }

        public ReturnStatement(string idName)
            : base(idName)
        {
        }
    }
}
