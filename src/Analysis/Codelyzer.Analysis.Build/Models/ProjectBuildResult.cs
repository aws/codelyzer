using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Codelyzer.Analysis.Build
{
    public class ProjectBuildResult : IDisposable
    {
        public string ProjectPath { get; set; }

        public string ProjectRootPath { get; set; }
        public List<string> SourceFiles { get; private set; }
        public List<SourceFileBuildResult> SourceFileBuildResults { get; private set; }
        public List<string> BuildErrors { get; set; }
        public Project Project { get; set; }
        public Compilation Compilation { get; set; }
        public ExternalReferences ExternalReferences { get; set; }
        public string TargetFramework { get; set; }
        public List<string> TargetFrameworks { get; set; }
        public string ProjectGuid { get; set; }        
        public string ProjectType { get; set; }
        public bool IsSyntaxAnalysis { get; set; }

        public ProjectBuildResult()
        {
            SourceFileBuildResults = new List<SourceFileBuildResult>();
            SourceFiles = new List<string>();
            TargetFrameworks = new List<string>();
        }

        public bool IsBuildSuccess()
        {
            return BuildErrors.Count == 0;
        }

        internal void AddSourceFile(string filePath)
        {
            var wsPath = Path.GetRelativePath(ProjectRootPath, filePath);
            SourceFiles.Add(wsPath);
        }

        public void Dispose()
        {
            Compilation = null;
        }
    }
}
