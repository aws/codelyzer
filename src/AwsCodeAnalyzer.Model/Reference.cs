using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class Reference
    {
        [JsonProperty("namespace", Order = 1)]
        public string Namespace { get; set; }
        [JsonProperty("assembly", Order = 2)]
        public string Assembly { get; set; }

        public override bool Equals(object obj)
        {
            Reference o = (Reference)obj;
            return this.Assembly == o.Assembly && this.Namespace == o.Namespace;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
