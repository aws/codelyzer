using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class MethodDeclaration : BaseMethodDeclaration
    {
        [JsonProperty("return-type", Order = 12)]
        public string ReturnType { get; set; }
        
        [JsonProperty("semantic-return-type", Order = 13)]
        public string SemanticReturnType { get; set; }

        public MethodDeclaration()
            : base(IdConstants.MethodIdName)
        {
        }
        public MethodDeclaration(string idName)
            : base(idName)
        {
        }
    }
}
