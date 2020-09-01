using System;
using System.IO;

namespace AwsCodeAnalyzer.Tests
{
    public class AwsBaseTest
    {
        private System.Type systemType;
        private string rootPath;

        protected void Setup(System.Type type)
        {
            this.systemType = type;
            this.rootPath = GetRootPath(type);
        }

        private string GetRootPath(System.Type type)
        {
            // The path will get normalized inside the .GetProject() call below
            string projectPath = Path.GetFullPath(
                Path.Combine(
                    Path.GetDirectoryName(type.Assembly.Location),
                    Path.Combine(new string[] { "..", "..", "..", ".." })));
            return projectPath;
        }

        public string GetPath(String path)
        {
            return Path.Combine(rootPath, path);
        }

        protected string GetPath(string[] pathStrings)
        {
            return Path.Combine(rootPath, Path.Combine(pathStrings));
        }
    }
    
    
}