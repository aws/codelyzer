using Codelyzer.Analysis.Common;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;

namespace Codelyzer.Analysis.Tests
{
    public class WorkspaceBaseTest
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

        protected void DownloadFromGitHub(string link, string name, string downloadsDir)
        {
            using (var client = new HttpClient())
            {
                var content = client.GetByteArrayAsync(link).Result;
                var fileName = Path.Combine(downloadsDir, string.Concat(name, @".zip"));
                File.WriteAllBytes(fileName, content);
                ZipFile.ExtractToDirectory(fileName, downloadsDir, true);
                File.Delete(fileName);
            }
        }

        protected void DeleteDir(string path, int retries = 0)
        {
            if (retries <= 10)
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
        }

        protected string CopySolutionFolderToTemp(string solutionName, string downloadsDir, string tempDir)
        {
            string solutionPath = Directory.EnumerateFiles(downloadsDir, 
                    solutionName,
                    SearchOption.AllDirectories)
                .FirstOrDefault() ?? string.Empty;
            string solutionDir = Directory.GetParent(solutionPath).FullName;
            var newTempDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
            FileUtils.DirectoryCopy(solutionDir, newTempDir);

            solutionPath = Directory.EnumerateFiles(newTempDir,
                solutionName,
                    SearchOption.AllDirectories)
                .FirstOrDefault() ?? string.Empty;
            return solutionPath;
        }

        protected async Task<Solution> GetWorkspaceSolution(string solutionPath)
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(solutionPath);
            return solution;
        }

        protected void SetupDefaultAnalyzerConfiguration(AnalyzerConfiguration configuration)
        {
            configuration.ExportSettings.GenerateJsonOutput = true;
            configuration.ExportSettings.OutputPath = Path.Combine("/", "tmp", "UnitTests");
            configuration.MetaDataSettings.LiteralExpressions = true;
            configuration.MetaDataSettings.MethodInvocations = true;
            configuration.MetaDataSettings.Annotations = true;
            configuration.MetaDataSettings.DeclarationNodes = true;
            configuration.MetaDataSettings.LocationData = false;
            configuration.MetaDataSettings.ReferenceData = true;
            configuration.MetaDataSettings.InterfaceDeclarations = true;
            configuration.MetaDataSettings.GenerateBinFiles = true;
            configuration.MetaDataSettings.LoadBuildData = true;
            configuration.MetaDataSettings.ReturnStatements = true;
            configuration.MetaDataSettings.InvocationArguments = true;
            configuration.MetaDataSettings.ElementAccess = true;
            configuration.MetaDataSettings.MemberAccess = true;
        }

        protected void SetupMsBuildLocator()
        {
            try
            {
                MSBuildLocator.RegisterDefaults();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
