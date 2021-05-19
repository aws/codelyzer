using Newtonsoft.Json;
using System;

namespace Codelyzer.Analysis.Model
{
    public class ExternalReference
    {
        [JsonProperty("identifier", Order = 1)]
        public string Identity { get; set; }
        [JsonProperty("version", Order = 2)]
        public string Version { get; set; }
        [JsonProperty("assembly-location", Order = 3)]
        public string AssemblyLocation { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ExternalReference);
        }

        public bool Equals(ExternalReference compare)
        {
            return compare != null &&
                Identity == compare.Identity &&
                Version == compare.Version &&
                AssemblyLocation == compare.AssemblyLocation;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Identity, Version, AssemblyLocation);
        }
    }
}
