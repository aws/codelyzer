using System.Collections.Generic;
using Newtonsoft.Json;
 
namespace Codelyzer.Analysis.Model
{
    public class ConstructorDeclaration : UstNode
    {
        [JsonProperty("modifiers", Order = 10)]
        public string Modifiers { get; set; }

        [JsonProperty("parameters", Order = 11)]
        public List<Parameter> Parameters { get; set; }

        [JsonProperty("semantic-properties", Order = 14)]
        public UstList<string> SemanticProperties { get; set; }
        public ConstructorDeclaration()
            : base(IdConstants.ConstructorIdName)
        {
            Parameters = new UstList<Parameter>();
            SemanticProperties = new UstList<string>();
        }
    }
}