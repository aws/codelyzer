using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class MethodBlockBase : UstNode
    {
        [JsonProperty("modifiers", Order = 10)]
        public string Modifiers { get; set; }

        [JsonProperty("parameters", Order = 11)]
        public List<Parameter> Parameters { get; set; }

        [JsonProperty("semantic-properties", Order = 14)]
        public UstList<string> SemanticProperties { get; set; }

        [JsonProperty("semantic-signature", Order = 30)]
        public string SemanticSignature { get; set; }

        protected MethodBlockBase(string idName)
            : base(idName)
        {
            Parameters = new UstList<Parameter>();
            SemanticProperties = new UstList<string>();
        }
        public override bool Equals(object obj)
        {
            if (obj is MethodBlockBase)
            {
                return Equals(obj as MethodBlockBase);
            }
            return false;
        }

        public bool Equals(MethodBlockBase compareNode)
        {
            return
                compareNode != null &&
                Modifiers?.Equals(compareNode.Modifiers) != false &&
                Parameters?.SequenceEqual(compareNode.Parameters) != false &&
                SemanticProperties?.SequenceEqual(compareNode.SemanticProperties) != false &&
                SemanticSignature?.Equals(compareNode.SemanticSignature) != false &&
                base.Equals(compareNode);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Modifiers, Parameters, SemanticProperties, SemanticSignature, base.GetHashCode());
        }
    }
}
