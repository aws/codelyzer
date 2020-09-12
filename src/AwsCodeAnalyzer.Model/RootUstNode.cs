using System.IO;
using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class RootUstNode : UstNode
    {
        public static readonly NodeType TYPE = new NodeType(IdConstants.RootId, 
            IdConstants.RootIdName);
        
        [JsonProperty("language", Order = 10)]
        public string Language { get; set; }
        
        [JsonProperty("file-path", Order = 11)]
        public string FilePath { get; set; }
        
        [JsonProperty("file-full-path", Order = 12)]
        public string FileFullPath { get; set; }

        [JsonProperty("references", Order = 99)]
        public UstList<Reference> References { get; set; }
        public RootUstNode() : base(TYPE.Name)
        {
            References = new UstList<Reference>();
        }

        public void SetPaths(string filePath, string fileFullPath)
        {
            FilePath = filePath;
            FileFullPath = fileFullPath;
        }
    }
}