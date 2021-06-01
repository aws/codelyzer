using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public abstract class BaseMethodDeclaration : UstNode
    {
        [JsonProperty("modifiers", Order = 10)]
        public string Modifiers { get; set; }

        [JsonProperty("parameters", Order = 11)]
        public List<Parameter> Parameters { get; set; }

        [JsonProperty("semantic-properties", Order = 14)]
        public UstList<string> SemanticProperties { get; set; }

        [JsonProperty("semantic-signature", Order = 30)]
        public string SemanticSignature { get; set; }

        protected BaseMethodDeclaration(string idName)
            : base(idName)
        {
            Parameters = new UstList<Parameter>();
            SemanticProperties = new UstList<string>();
        }
        public override bool Equals(object obj)
        {
            if (obj is BaseMethodDeclaration)
            {
                return Equals(obj as BaseMethodDeclaration);
            }
            return false;
        }

        public bool Equals(BaseMethodDeclaration compareNode)
        {
            return
                compareNode != null &&
                Modifiers?.Equals( compareNode.Modifiers ) != false &&
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
