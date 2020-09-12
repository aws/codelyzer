using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class Annotation : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.AnnotationId,
            IdConstants.AnnotationIdName);

        [JsonProperty("semantic-class-type", Order = 14)]
        public string SemanticClassType { get; set; }
        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public Annotation()
            : base(TYPE.Name)
        {
            Reference = new Reference();
        }
    }
}