using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class Annotation : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.AnnotationId,
            IdConstants.AnnotationIdName);

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public Annotation()
            : base(TYPE.Name)
        {
            Reference = new Reference();
        }
    }
}