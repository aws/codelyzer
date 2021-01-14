using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
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

        [Obsolete(Constants.ObsoleteParameterMessage, Constants.DoNotThrowErrorOnUse)]
        [JsonProperty("parameters", Order = 30)]
        public List<Parameter> Parameters { get; set; }

        [JsonProperty("arguments", Order = 31)] 
        public List<Argument> Arguments { get; set; }
        
        [JsonProperty("semantic-return-type", Order = 35)]
        public string SemanticReturnType { get; set; }

        [JsonProperty("semantic-original-def", Order = 40)]
        public string SemanticOriginalDefinition { get; set; }
        
        [JsonProperty("semantic-properties", Order = 45)]
        public List<string> SemanticProperties { get; set; }

        [JsonProperty("semantic-is-extension", Order = 50)]
        public bool IsExtension { get; set; }

        [JsonProperty("references", Order = 99)]
        public Reference Reference { get; set; }

        public InvocationExpression(string typeName)
            : base(typeName)
        {
            SemanticProperties = new List<string>();
            Parameters = new List<Parameter>();
            Arguments = new List<Argument>();
            Reference = new Reference();
        }
        
        public InvocationExpression()
            : base(IdConstants.InvocationIdName)
        {
            SemanticProperties = new List<string>();
            Parameters = new List<Parameter>();
            Arguments = new List<Argument>();
            Reference = new Reference();
        }
    }
}
