using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Codelyzer.Analysis.Model
{
    public class PropertyStatement : UstNode
    {
        [JsonProperty("base-type", Order = 10)]
        public string BaseType { get; set; }

        [JsonProperty("base-type-original-def", Order = 11)]
        public string BaseTypeOriginalDefinition { get; set; }

        [JsonProperty("base-list", Order = 12)]
        public List<string> BaseList { get; set; }

        [JsonProperty("modifiers", Order = 20)]
        public string Modifiers { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public string SemanticAssembly { get; set; }

        public PropertyStatement()
            : base(IdConstants.PropertyStatementName)
        {
            Reference = new Reference();
        }

        public override bool Equals(object obj)
        {
            if (obj is PropertyStatement)
            {
                return Equals(obj as PropertyStatement);
            }
            return false;
        }

        public bool Equals(PropertyStatement compareNode)
        {
            return
                compareNode != null &&
                BaseType?.Equals(compareNode.BaseType) != false &&
                BaseTypeOriginalDefinition?.Equals(compareNode.BaseTypeOriginalDefinition) != false &&
                Modifiers?.Equals(compareNode.Modifiers) != false &&
                SemanticAssembly?.Equals(compareNode.SemanticAssembly) != false &&
                base.Equals(compareNode);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(BaseType, BaseTypeOriginalDefinition, Modifiers, SemanticAssembly, base.GetHashCode());
        }
    }
}
