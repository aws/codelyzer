using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class InvocationExpression : ExpressionStatement
    {        
        [JsonProperty("method-name", Order = 10)]
        public string MethodName { get; set; }
        
        [JsonProperty("modifiers", Order = 11)]
        public string Modifiers { get; set; }
        
        [JsonProperty("semantic-namespace", Order = 12)]
        public string SemanticNamespace { get; set; }
        
        [JsonProperty("caller-identifier", Order = 13) ]
        public string CallerIdentifier { get; set; }
        
        [JsonProperty("semantic-class-type", Order = 14)]
        public string SemanticClassType { get; set; }
        
        [JsonProperty("semantic-method-signature", Order = 15)]
        public string SemanticMethodSignature { get; set; }
        
        [JsonProperty("parameters", Order = 16)] 
        public List<Parameter> Parameters { get; set; }
        
        [JsonProperty("semantic-return-type", Order = 17)]
        public string SemanticReturnType { get; set; }

        [JsonProperty("semantic-original-def", Order = 18)]
        public string SemanticOriginalDefinition { get; set; }
        
        [JsonProperty("semantic-properties", Order = 19)]
        public List<string> SemanticProperties { get; set; }

        [JsonProperty("semantic-is-extension", Order = 20)]
        public bool IsExtension { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }

        public InvocationExpression(string typeName)
            : base(typeName)
        {
            SemanticProperties = new List<string>();
        }
        
        public InvocationExpression()
            : base(IdConstants.InvocationIdName)
        {
            SemanticProperties = new List<string>();
            Parameters = new List<Parameter>();
            Reference = new Reference();
        }
    }
}