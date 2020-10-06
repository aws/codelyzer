using System.Collections.Generic;
using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class MethodDeclaration : UstNode
    {
        [JsonProperty("modifiers", Order = 10)]
        public string Modifiers { get; set; }

        [JsonProperty("parameters", Order = 11)] 
        public List<Parameter> Parameters { get; set; }

        [JsonProperty("return-type", Order = 12)]
        public string ReturnType { get; set; }
        
        [JsonProperty("semantic-return-type", Order = 13)]
        public string SemanticReturnType { get; set; }
        
        [JsonProperty("semantic-properties", Order = 14)]
        public UstList<string> SemanticProperties { get; set; }
        public MethodDeclaration()
            : base(IdConstants.MethodIdName)
        {
            Parameters = new UstList<Parameter>();
            SemanticProperties = new UstList<string>();
        }
    }
}