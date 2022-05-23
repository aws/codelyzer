using Newtonsoft.Json;
using System;
namespace Codelyzer.Analysis.Model
{
    public class AttributeList : UstNode
    {
        [JsonProperty("semantic-class-type", Order = 14)]
        public string SemanticClassType { get; set; }
        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public AttributeList()
            : base(IdConstants.AttributeListName)
        {
            Reference = new Reference();
        }

        public override bool Equals(object obj)
        {
            if (obj is AttributeList)
            {
                return Equals(obj as AttributeList);
            }
            return false;
        }

        public bool Equals(AttributeList compareNode)
        {
            return
                compareNode != null &&
                Reference?.Equals(compareNode.Reference) != false &&
                SemanticClassType?.Equals(compareNode.SemanticClassType) != false &&
                base.Equals(compareNode);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(SemanticClassType, base.GetHashCode());
        }
    }
}
