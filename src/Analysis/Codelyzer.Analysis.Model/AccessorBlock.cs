using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Codelyzer.Analysis.Model
{
    public class AccessorBlock : UstNode
    {
        [JsonProperty("modifiers", Order = 20)]
        public string Modifiers { get; set; }

        public AccessorBlock()
            : base(IdConstants.AccessorBlockName)
        {

        }
    }
}
