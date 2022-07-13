using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Codelyzer.Analysis.Model
{
    public class PropertyBlock : UstNode
    {
        [JsonProperty("modifiers", Order = 20)]
        public string Modifiers { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public string SemanticAssembly { get; set; }

        [JsonProperty("parameters", Order = 100)]
        public List<Parameter> Parameters { get; set; }

        public PropertyBlock()
            : base(IdConstants.PropertyBlockName)
        {
            Reference = new Reference();
        }

        public override bool Equals(object obj)
        {
            if (obj is PropertyBlock)
            {
                return Equals(obj as PropertyBlock);
            }
            return false;
        }
    }
}
