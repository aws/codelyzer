using System;
using System.IO;
using Newtonsoft.Json;

namespace Codelyzer.Analysis.Model
{
    public class RootUstNode : UstNode
    {        
        [JsonProperty("language", Order = 10)]
        public string Language { get; set; }
        
        [JsonProperty("file-path", Order = 11)]
        public string FilePath { get; set; }
        
        [JsonProperty("file-full-path", Order = 12)]
        public string FileFullPath { get; set; }

        [JsonProperty("lines-of-code", Order = 13)]
        public int LinesOfCode { get; set; }

        [JsonProperty("references", Order = 99)]
        public UstList<Reference> References { get; set; }
        public RootUstNode() : base(IdConstants.RootIdName)
        {
            References = new UstList<Reference>();
        }

        public void SetPaths(string filePath, string fileFullPath)
        {
            FilePath = filePath;
            FileFullPath = fileFullPath;
        }

        public override bool Equals(object obj)
        {
            if (obj is RootUstNode)
            {
                return Equals((RootUstNode)obj);
            }
            else return false;
        }

        public bool Equals(RootUstNode compareNode)
        {
            return compareNode != null &&
                Language?.Equals(compareNode.Language) != false
                && FilePath?.Equals(compareNode.FilePath) != false
                && FileFullPath?.Equals(compareNode.FileFullPath) != false
                && base.Equals(compareNode);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Language, FilePath, FileFullPath, base.GetHashCode());
        }
    }
}
