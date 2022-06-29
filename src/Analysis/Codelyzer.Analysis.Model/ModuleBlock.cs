﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Codelyzer.Analysis.Model
{
    public class ModuleBlock : BaseMethodDeclaration
    {
        [JsonProperty("base-type", Order = 10)]
        public string BaseType { get; set; }

        [JsonProperty("base-type-original-def", Order = 11)]
        public string BaseTypeOriginalDefinition { get; set; }

        [JsonProperty("base-list", Order = 12)]
        public List<string> BaseList { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }
        public string SemanticAssembly { get; set; }

        public ModuleBlock()
            : base(IdConstants.ModuleBlockName)
        {
            Reference = new Reference();
        }

        public override bool Equals(object obj)
        {
            if (obj is ModuleBlock)
            {
                return Equals(obj as ModuleBlock);
            }
            return false;
        }

        public bool Equals(ModuleBlock compareNode)
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
