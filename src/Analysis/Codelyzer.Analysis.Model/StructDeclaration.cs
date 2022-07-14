using Newtonsoft.Json;
using System.Collections.Generic;

namespace Codelyzer.Analysis.Model
{
    public class StructDeclaration : UstNode
    {
        [JsonProperty("base-type", Order = 10)]
        public string BaseType { get; set; }

        [JsonProperty("base-type-original-def", Order = 11)]
        public string BaseTypeOriginalDefinition { get; set; }

        [JsonProperty("base-list", Order = 12)]
        public List<string> BaseList { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public StructDeclaration()
            : base(IdConstants.StructIdName)
        {
            Reference = new Reference();
        }
    }
}
