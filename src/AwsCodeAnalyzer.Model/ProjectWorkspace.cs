using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AwsCodeAnalyzer.Model
{
    public class ProjectWorkspace
    {
        [JsonProperty("version", Order = 1)] 
        public string Version { get; set; } = "1.0";

        [JsonProperty("generated-by", Order = 2)]
        public string GeneratedBy { get; set; }

        [JsonProperty("workspace-name", Order = 3)]
        public string ProjectName { get; }
        
        [JsonProperty("workspace-root-path", Order = 4)]
        public string ProjectRootPath { get; }

        [JsonProperty("source-files", Order = 5)]
        public UstList<string> SourceFiles;

        [JsonProperty("errors-found", Order = 6)]
        public int BuildErrorsCount { get; set; }

        [JsonProperty("target-framework", Order = 7)]
        public string TargetFramework { get; set; }

        [JsonProperty("external-references", Order = 8)]
        public ExternalReferences ExternalReferences { get; set; }

        [JsonProperty("source-file-results", Order = 9)]
        public UstList<RootUstNode> SourceFileResults;

        [JsonProperty("workspace-path", Order = 10)]
        public string ProjectFilePath { get; }
        
        [JsonProperty("build-errors", Order = 11)]
        public List<String> BuildErrors { get; set; }

        public ProjectWorkspace(string projectFilePath)
        {
            SourceFiles = new UstList<string>();
            ProjectFilePath = projectFilePath;
            ProjectRootPath = Path.GetDirectoryName(projectFilePath);
            ProjectName = Path.GetFileNameWithoutExtension(projectFilePath);
            SourceFileResults = new UstList<RootUstNode>();
            GeneratedBy = "Auto generated by the Codelyzer  on: " + 
                          DateTime.Now.ToString("dddd, dd MMMM yyyy") ;
        }
    }
}