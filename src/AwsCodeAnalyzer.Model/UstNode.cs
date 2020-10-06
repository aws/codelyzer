using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AwsCodeAnalyzer.Model
{   
    public partial class UstNode
    {
        [JsonIgnore]
        public readonly NodeType TYPE;

        [JsonProperty("type", Order = 1)]
        public string NodeType { get => TYPE.Name; }
        
        [JsonProperty("identifier", Order = 2)]
        public string Identifier { get; set; }
        
        [JsonProperty("location", Order = 3)]
        public TextSpan TextSpan { get; set; }

        [JsonIgnore]
        public UstNode Parent { get; set; }

        [JsonProperty("children", Order = 100)]
        public UstList<UstNode> Children { get; set; }

        public UstNode(int nodeId, string nodeType)
        {
            TYPE = new NodeType(nodeId, nodeType);
            Children = new UstList<UstNode>();
        }
    }

    public partial class NodeType
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
        
        public NodeType(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public partial class TextSpan
    {
        [JsonProperty("start-char-position")]
        public long StartCharPosition { get; set; }

        [JsonProperty("end-char-position")]
        public long EndCharPosition { get; set; }

        [JsonProperty("start-line-position")]
        public long StartLinePosition { get; set; }

        [JsonProperty("end-line-position")]
        public long EndLinePosition { get; set; }
    }

    public partial class Parameter
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("semantic-type")]
        public string SemanticType { get; set; }
    }
    

    public partial class UstNode
    {
        public static UstNode FromJson(string json) => JsonConvert.DeserializeObject<UstNode>(json, AwsCodeAnalyzer.Model.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this UstNode self) => JsonConvert.SerializeObject(self, AwsCodeAnalyzer.Model.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}