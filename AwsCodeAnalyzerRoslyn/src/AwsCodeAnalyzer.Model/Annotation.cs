using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class Annotation : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.AnnotationId,
            IdConstants.AnnotationIdName);

        [JsonProperty("semantic-namespace")]
        public string SemanticNamespace { get; set; }

        public Annotation()
            : base(TYPE.Name)
        {
        }
    }
}