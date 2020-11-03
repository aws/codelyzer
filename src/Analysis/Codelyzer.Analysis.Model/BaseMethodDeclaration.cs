using System.Collections.Generic;
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

        protected BaseMethodDeclaration(string idName)
            : base(idName)
        {
            Parameters = new UstList<Parameter>();
            SemanticProperties = new UstList<string>();
        }
    }
}
