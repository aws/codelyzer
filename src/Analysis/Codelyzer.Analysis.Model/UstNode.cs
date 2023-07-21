using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Codelyzer.Analysis.Model
{   
    public partial class UstNode
    {
        [JsonIgnore]
        public readonly string type;

        [JsonProperty("type", Order = 1)]
        public string NodeType { get => type; }
        
        [JsonProperty("identifier", Order = 2)]
        public string Identifier { get; set; }
        
        [JsonProperty("location", Order = 3)]
        public TextSpan TextSpan { get; set; }

        [JsonProperty("parent-node", Order = 4)]
        public UstNode Parent { get; set; }

        [JsonProperty("full-identifier", Order = 5)]
        public string FullIdentifier { get; set; }

        [JsonProperty("children", Order = 100)]
        public UstList<UstNode> Children { get; set; }

        public UstNode(string nodeType)
        {
            type = nodeType;
            Children = new UstList<UstNode>();
        }

        public override bool Equals(object obj)
        {
            if (obj is UstNode)
            {
                return Equals((UstNode)obj);
            }
            else return false;
        }

        public bool Equals(UstNode compareNode)
        {
            return (NodeType?.Equals(compareNode.NodeType) != false)
                && (Identifier?.Equals(compareNode.Identifier) != false)
                && (TextSpan?.Equals(compareNode.TextSpan) != false)
                && (Children?.Equals(compareNode.Children) != false);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NodeType, Identifier, TextSpan, Parent, Children);
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

        public override bool Equals(object obj)
        {
            return Equals((TextSpan)obj);
        }

        public bool Equals(TextSpan compareSpan)
        {
            return compareSpan != null &&
                StartCharPosition == compareSpan.StartCharPosition
                && EndCharPosition == compareSpan.EndCharPosition
                && StartLinePosition == compareSpan.StartLinePosition
                && EndLinePosition == compareSpan.EndLinePosition;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartCharPosition, EndCharPosition, StartLinePosition, EndLinePosition);
        }
    }

    public partial class Parameter
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("semantic-type")]
        public string SemanticType { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is Parameter)
            {
                return Equals(obj as Parameter);
            }
            return false;
        }

        public bool Equals(Parameter compareNode)
        {
            return
                compareNode != null &&
                Name?.Equals(compareNode.Name) != false &&
                Type?.Equals(compareNode.Type) != false &&
                SemanticType?.Equals(compareNode.SemanticType) != false;

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Type, SemanticType);
        }
    }
    

    public partial class UstNode
    {
        public static UstNode FromJson(string json) => JsonConvert.DeserializeObject<UstNode>(json, Codelyzer.Analysis.Model.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this UstNode self) => JsonConvert.SerializeObject(self, Codelyzer.Analysis.Model.Converter.Settings);
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
