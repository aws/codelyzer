using Newtonsoft.Json;
using System;

namespace Codelyzer.Analysis.Model
{
    public class MemberAccess : UstNode
    {
        [JsonProperty("name", Order = 10)]
        public string Name { get; set; }

        [JsonProperty("expression", Order = 11)]
        public string Expression { get; set; }

        [JsonProperty("semantic-class-type", Order = 14)]
        public string SemanticClassType { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }

        public MemberAccess()
            : base(IdConstants.MemberAccessIdName)
        {
            Reference = new Reference();
        }

        public MemberAccess(string idName)
            : base(idName)
        {
            Reference = new Reference();
        }
        public override bool Equals(object obj)
        {
            if (obj is MemberAccess)
            {
                return Equals(obj as MemberAccess);
            }
            return false;
        }

        public bool Equals(MemberAccess compareNode)
        {
            return
                compareNode != null &&
                Name?.Equals(compareNode.Name) != false &&
                Expression?.Equals(compareNode.Expression) != false &&
                SemanticClassType?.Equals(compareNode.SemanticClassType) != false &&
                base.Equals(compareNode);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Expression, SemanticClassType, base.GetHashCode());
        }
    }
}
