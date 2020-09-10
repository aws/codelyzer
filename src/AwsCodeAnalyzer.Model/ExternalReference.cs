using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AwsCodeAnalyzer.Model
{
    public class ExternalReference
    {
        [JsonProperty("identifier", Order = 1)]
        public string Identity { get; set; }
        [JsonProperty("version", Order = 2)]
        public string Version { get; set; }
        [JsonProperty("assembly-location", Order = 3)]
        public string AssemblyLocation { get; set; }
    }
}
