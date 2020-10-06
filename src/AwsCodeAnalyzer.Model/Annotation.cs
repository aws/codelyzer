using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class Annotation : UstNode
    {
        [JsonProperty("semantic-class-type", Order = 14)]
        public string SemanticClassType { get; set; }
        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public Annotation()
            : base(IdConstants.AnnotationId,
            IdConstants.AnnotationIdName)
        {
            Reference = new Reference();
        }
    }
}