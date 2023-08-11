using Amazon.Lambda.Core;
using LambdaSharp.Serialization;
using Newtonsoft.Json;
using System.IO;
using System;

namespace DataExtractionCommon
{
    public class SerializationException : Exception
    {
        public SerializationException(string message) : base(message)
        {
        }
    }

    public class GraphSerializer : ILambdaSerializer
    {
        public GraphSerializer() : base()
        {
        }

        public static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.All,
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            MaxDepth = 10000
        };

        public T Deserialize<T>(Stream requestStream)
        {
            try
            {
                var val = new StreamReader(requestStream).ReadToEnd();
                return JsonConvert.DeserializeObject<T>(val, JsonSerializerSettings);
            }
            catch (Exception e)
            {
                throw new SerializationException($"Failed to deserialize object from stream: {e.Message}");
            }
        }

        public void Serialize<T>(T response, Stream responseStream)
        {
            try
            {
                var result = JsonConvert.SerializeObject(response, JsonSerializerSettings);
                var streamWriter = new StreamWriter(responseStream);
                streamWriter.Write(result);
                streamWriter.Flush();
            }
            catch (Exception e)
            {
                throw new SerializationException($"Failed to serialize object to stream: {e.Message}");
            }
        }
    }
}