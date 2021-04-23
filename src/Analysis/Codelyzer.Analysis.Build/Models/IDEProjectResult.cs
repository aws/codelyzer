using Codelyzer.Analysis.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Codelyzer.Analysis.Build
{
    public class IDEProjectResult
    {
        public IDEProjectResult()
        {
            SourceFileBuildResults = new List<SourceFileBuildResult>();
            RootNodes = new List<RootUstNode>();
        }
        public string ProjectPath { get; set; }
        public List<SourceFileBuildResult> SourceFileBuildResults { get; set; }
        public List<RootUstNode> RootNodes { get; set; }
    }
}
