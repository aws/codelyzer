using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Assert;

namespace Codelyzer.Analysis.Tests
{
    //Implementations

    [TestFixture()]
    [NonParallelizable]
    public class AwsAnalyzerTests : AwsBaseTest
    {
        public string tempDir = "";

        [SetUp]
        public void Setup()
        {
            Setup(this.GetType());
            tempDir = GetTstPath(Path.Combine(new string[] { "Projects", "Temp" }));
            DownloadTestProjects();
        }

        private void DownloadTestProjects()
        {
            var tempDirectory = Directory.CreateDirectory(tempDir);
            //var fileName = Path.Combine(tempDirectory.Parent.FullName, @"TestProjects.zip");
            //CommonUtils.SaveFileFromGitHub(fileName, GithubInfo.TestGithubOwner, GithubInfo.TestGithubRepo, GithubInfo.TestGithubTag);
            //ZipFile.ExtractToDirectory(fileName, tempDirectory.FullName, true);
            ////Move file to a shorter dir name so it can build:
            //var dirs = Directory.EnumerateDirectories(tempDirectory.FullName).FirstOrDefault();
            //File.Delete(fileName);

            ////Need to move because downloaded file name is long, and wouldn't build on windows
            //var destDir = Path.Combine(tempDirectory.FullName, "TestProjects");
            //if (Directory.Exists(destDir))
            //{
            //    Directory.Delete(destDir, true);
            //}

            DownloadFromGitHub(@"https://github.com/FabianGosebrink/ASPNET-WebAPI-Sample/archive/master.zip", "ASPNET-WebAPI-Sample-master");
            DownloadFromGitHub(@"https://github.com/Duikmeester/MvcMusicStore/archive/master.zip", "MvcMusicStore-master");

            //Directory.Move(dirs, Path.Combine(tempDirectory.FullName, "TestProjects"));
        }


        private void DownloadFromGitHub(string link, string name)
        {
            using (var client = new HttpClient())
            {
                var content = client.GetByteArrayAsync(link).Result;
                var tempDirectory = Directory.CreateDirectory(GetTstPath(Path.Combine(new string[] { "Projects", "Temp" })));
                var fileName = string.Concat(tempDirectory.FullName, name, @".zip");
                File.WriteAllBytes(fileName, content);
                ZipFile.ExtractToDirectory(fileName, tempDirectory.FullName, true);
                File.Delete(fileName);
            }
        }

        [Test]
        public async Task TestCli()
        {
            string mvcMusicStorePath = Directory.EnumerateFiles(tempDir, "MvcMusicStore.sln", SearchOption.AllDirectories).FirstOrDefault();
            var sampleFilePath = GetSrcPath(Path.Combine("Analysis", "Codelyzer.Analysis", "sample_input.json"));
            string[] args = new string[] { "-o  ", sampleFilePath, "-s", mvcMusicStorePath };
            AnalyzerCLI cli = new AnalyzerCLI();
            cli.HandleCommand(args);
            Assert.NotNull(cli);
            Assert.NotNull(cli.FilePath);
            Assert.NotNull(cli.Project);
            Assert.NotNull(cli.Configuration);
        }

        [Test]
        public async Task TestAnalyzer()
        {
            string projectPath = string.Concat(GetTstPath(Path.Combine(new string[] { "Projects", "CodelyzerDummy", "CodelyzerDummy" })), ".csproj");

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                ExportSettings =
                {
                    GenerateJsonOutput = true,
                    GenerateGremlinOutput = false,
                    GenerateRDFOutput = false,
                    OutputPath = @"/tmp/UnitTests"
                },

                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true,
                    Annotations = true,
                    DeclarationNodes = true,
                    LocationData = true,
                    ReferenceData  = true,
                    LoadBuildData = true
                }
            };
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, NullLogger.Instance);
            using AnalyzerResult result = await analyzer.AnalyzeProject(projectPath);
            Assert.True(result != null);
        }



        [Test]
        public async Task TestSampleWebApi()
        {
            string solutionPath = Directory.EnumerateFiles(tempDir, "SampleWebApi.sln", SearchOption.AllDirectories).FirstOrDefault();
            FileAssert.Exists(solutionPath);

            string solutionDir = Directory.GetParent(solutionPath).FullName;

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                ExportSettings =
                {
                    GenerateJsonOutput = true,
                    OutputPath = Path.Combine("/", "tmp", "UnitTests")
                },

                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true,
                    Annotations = true,
                    DeclarationNodes = true,
                    LocationData = false,
                    ReferenceData = true
                }
            };
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, NullLogger.Instance);
            AnalyzerResult result = (await analyzer.AnalyzeSolution(solutionPath)).FirstOrDefault();
            Assert.True(result != null);

            //Project has 16 nuget references and 19 framework/dll references:
            Assert.AreEqual(result.ProjectResult.ExternalReferences.NugetReferences.Count, 16);
            Assert.AreEqual(result.ProjectResult.ExternalReferences.SdkReferences.Count, 19);

            Assert.AreEqual(result.ProjectResult.SourceFiles.Count, 10);

            var houseController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HouseController.cs")).FirstOrDefault();
            Assert.NotNull(houseController);

            var blockStatements = houseController.AllBlockStatements();
            var classDeclarations = houseController.AllClasses();
            var expressionStatements = houseController.AllExpressions();
            var invocationExpressions = houseController.AllInvocationExpressions();
            var literalExpressions = houseController.AllLiterals();
            var methodDeclarations = houseController.AllMethods();
            var namespaceDeclarations = houseController.AllNamespaces();
            var objectCreationExpressions = houseController.AllObjectCreationExpressions();
            var usingDirectives = houseController.AllUsingDirectives();


            Assert.AreEqual(blockStatements.Count, 6);
            Assert.AreEqual(classDeclarations.Count, 1);
            Assert.AreEqual(expressionStatements.Count, 51);
            Assert.AreEqual(invocationExpressions.Count, 41);
            Assert.AreEqual(literalExpressions.Count,10);
            Assert.AreEqual(namespaceDeclarations.Count, 1);
            Assert.AreEqual(objectCreationExpressions.Count, 0);
            Assert.AreEqual(usingDirectives.Count, 10);
        }

        [Test]
        public async Task TestMvcMusicStore()
        {
            string solutionPath = Directory.EnumerateFiles(tempDir, "MvcMusicStore.sln", SearchOption.AllDirectories).FirstOrDefault();
            FileAssert.Exists(solutionPath);

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                ExportSettings =
                {
                    GenerateJsonOutput = false,
                    OutputPath = @"/tmp/UnitTests"
                },

                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true,
                    Annotations = true,
                    DeclarationNodes = true,
                    LocationData = false,
                    ReferenceData = true
                }
            };
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, NullLogger.Instance);
            using var result = (await analyzer.AnalyzeSolution(solutionPath)).FirstOrDefault();
            Assert.True(result != null);

            Assert.AreEqual(result.ProjectResult.SourceFiles.Count, 28);

            //Project has 16 nuget references and 19 framework/dll references:
            Assert.AreEqual(result.ProjectResult.ExternalReferences.NugetReferences.Count, 29);
            Assert.AreEqual(result.ProjectResult.ExternalReferences.SdkReferences.Count, 24);

            var homeController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HomeController.cs")).FirstOrDefault();
            Assert.NotNull(homeController);

            var classDeclarations = homeController.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault();
            Assert.Greater(classDeclarations.Children.Count, 0);

            var classDeclaration = homeController.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault().Children[0];
            Assert.NotNull(classDeclaration);

            var declarationNodes = classDeclaration.Children.OfType<Codelyzer.Analysis.Model.DeclarationNode>();
            var methodDeclarations = classDeclaration.Children.OfType<Model.MethodDeclaration>();

            //HouseController has 3 identifiers declared within the class declaration:
            Assert.AreEqual(declarationNodes.Count(), 4);

            //It has 2 method declarations
            Assert.AreEqual(methodDeclarations.Count(), 2);
        }

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(GetTstPath(Path.Combine("Projects", "Temp")), true);
        }
    }
}
