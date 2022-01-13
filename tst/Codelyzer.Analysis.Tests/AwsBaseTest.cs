using System.IO;

namespace Codelyzer.Analysis.Tests
{
    public class AwsBaseTest
    {
        private System.Type systemType;
        private string tstPath;
        private string srcPath;

        protected void Setup(System.Type type)
        {
            this.systemType = type;
            this.tstPath = GetTstPath(type);
            this.srcPath = GetSrcPath(type);
        }

        private string GetTstPath(System.Type type)
        {
            // The path will get normalized inside the .GetProject() call below
            string projectPath = Path.GetFullPath(
                Path.Combine(
                    Path.GetDirectoryName(type.Assembly.Location),
                    Path.Combine(new string[] { "..", "..", "..", ".." })));
            return projectPath;
        }

        private string GetSrcPath(System.Type type)
        {
            // The path will get normalized inside the .GetProject() call below
            string projectPath = Path.GetFullPath(
                Path.Combine(
                    Path.GetDirectoryName(type.Assembly.Location),
                    Path.Combine(new string[] { "..", "..", "..", "..", "..", "src" })));
            return projectPath;
        }

        public string GetTstPath(string path)
        {
            return Path.Combine(tstPath, path);
        }
        public string GetSrcPath(string path)
        {
            return Path.Combine(srcPath, path);
        }
    }
}
