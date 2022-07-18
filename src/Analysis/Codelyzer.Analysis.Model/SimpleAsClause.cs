using Newtonsoft.Json;
using System;
using System.Collections.Generic;
namespace Codelyzer.Analysis.Model
{
    public class SimpleAsClause : UstNode
    {
        [JsonProperty("type", Order = 10)]
        public string Type { get; set; }
        public SimpleAsClause()
            : base(IdConstants.SimpleAsClauseName)
        {
        }
    }
}
