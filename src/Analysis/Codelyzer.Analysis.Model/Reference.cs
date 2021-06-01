using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System;

namespace Codelyzer.Analysis.Model
{
    public class Reference
    {
        [JsonProperty("namespace", Order = 1)]
        public string Namespace { get; set; }
        [JsonProperty("assembly", Order = 2)]
        public string Assembly { get; set; }        
        [JsonProperty("assembly-location", Order = 3)]
        public string AssemblyLocation { get; set; }
        [JsonProperty("version", Order = 4)]
        public string Version { get; set; }

        [JsonIgnore]
        public IAssemblySymbol AssemblySymbol { get; set; }

        public override bool Equals(object obj)
        {
            Reference o = (Reference)obj;
            return this.Assembly == o.Assembly && this.Namespace == o.Namespace;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Namespace, Assembly);
        }
    }
}
