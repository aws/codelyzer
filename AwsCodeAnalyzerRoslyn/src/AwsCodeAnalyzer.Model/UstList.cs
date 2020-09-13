using System.Collections.Generic;
using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    [JsonArray]
    public class UstList<T> : List<T>
    {
        public UstList()
        {
        }

        public UstList(IEnumerable<T> collection) : base(collection)
        {
        }

        public UstList(int capacity) : base(capacity)
        {
        }
    }
}