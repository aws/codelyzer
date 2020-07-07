using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AwsCodeAnalyzer.Common
{
    public static class SerializeUtils
    {
        public static string ToJson<T>(T obj) => JsonConvert.SerializeObject(obj, Converter.Settings);
        
        public static T FromJson<T>(string json) => JsonConvert.DeserializeObject<T>(json, Converter.Settings);

    }
    
    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}