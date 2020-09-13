using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class ClassDeclaration : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.ClassId, 
            IdConstants.ClassIdName);

        [JsonProperty("base-type", Order = 10)]
        public string BaseType { get; set; }
        public ClassDeclaration()
            : base(TYPE.Name)
        {
        }
    }
}