using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Codelyzer.Analysis.Model
{
    public class InterfaceDeclaration : UstNode
    {
        /// The list of all interfaces of which this type is a declared subtype, excluding this type
        /// itself. This includes all declared base interfaces, all declared base interfaces of base
        /// types, and all declared base interfaces of those results (recursively).
        [JsonIgnore]
        public List<InterfaceDeclaration> AllBaseTypeDeclarationList { get; set; }
        [JsonProperty("base-type", Order = 10)]
        public string BaseType { get; set; }

        [JsonProperty("base-type-original-def", Order = 11)]
        public string BaseTypeOriginalDefinition { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public string SemanticAssembly { get; set; }
        public InterfaceDeclaration()
            : base(IdConstants.InterfaceIdName)
        {
            Reference = new Reference();
        }
        public override bool Equals(object obj)
        {
            if (obj is InterfaceDeclaration)
            {
                return Equals(obj as InterfaceDeclaration);
            }
            return false;
        }

        public bool Equals(InterfaceDeclaration compareNode)
        {
            return
                compareNode != null &&
                BaseType?.Equals(compareNode.BaseType) != false &&
                BaseTypeOriginalDefinition?.Equals(compareNode.BaseTypeOriginalDefinition) != false &&
                SemanticAssembly?.Equals(compareNode.SemanticAssembly) != false &&
                base.Equals(compareNode);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(BaseType, BaseTypeOriginalDefinition, SemanticAssembly, base.GetHashCode());
        }
    }
}
