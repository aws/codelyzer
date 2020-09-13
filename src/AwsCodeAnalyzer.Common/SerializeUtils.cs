using System;
using System.Globalization;
using AwsCodeAnalyzer.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace AwsCodeAnalyzer.Common
{
    public static class SerializeUtils
    {
        public static string ToJson<T>(T obj) => JsonConvert.SerializeObject(obj, Converter.Settings);
        
        public static T FromJson<T>(string json) => JsonConvert.DeserializeObject<T>(json, Converter.Settings);

    }

    public class AnalyzerConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(UstNode).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, 
            Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            var type = jObject["type"];

            if (type != null)
            { 
                string ustType = type.ToString();
                UstNode item = ModelFactory.GetObject(ustType);
                serializer.Populate(jObject.CreateReader(), item);
                return item;
            }
            else
            {
                ProjectWorkspace item = new ProjectWorkspace("");
                serializer.Populate(jObject.CreateReader(), item);
                return item;
            }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, 
            object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    
    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal },
                new AnalyzerConverter()
            },
        };
    }
}