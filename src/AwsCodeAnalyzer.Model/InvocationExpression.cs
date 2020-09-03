using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class InvocationExpression : ExpressionStatement
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.InvocationId, 
            IdConstants.InvocationIdName);
        
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
        [JsonProperty("semantic-assembly", Order = 21)]
        public string SemanticAssembly { get; set; }

        public InvocationExpression(NodeType type)
            : base(type)
        {
            SemanticProperties = new List<string>();
        }
        
        public InvocationExpression()
            : base(TYPE)
        {
            SemanticProperties = new List<string>();
            Parameters = new List<Parameter>();
        }
    }
}