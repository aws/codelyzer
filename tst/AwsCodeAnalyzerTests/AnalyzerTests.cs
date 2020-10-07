using AwsCodeAnalyzer.Common;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Assert;

namespace AwsCodeAnalyzer.Tests
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
            var fileName = Path.Combine(tempDirectory.Parent.FullName, @"TestProjects.zip");
            CommonUtils.SaveFileFromGitHub(fileName, GithubInfo.TestGithubOwner, GithubInfo.TestGithubRepo, GithubInfo.TestGithubTag);
            ZipFile.ExtractToDirectory(fileName, tempDirectory.FullName, true);
            //Move file to a shorter dir name so it can build:
            var dirs = Directory.EnumerateDirectories(tempDirectory.FullName).FirstOrDefault();
            File.Delete(fileName);

            //Need to move because downloaded file name is long, and wouldn't build on windows
            var destDir = Path.Combine(tempDirectory.FullName, "TestProjects");
            if (Directory.Exists(destDir))
            {
                Directory.Delete(destDir, true);
            }
            Directory.Move(dirs, Path.Combine(tempDirectory.FullName, "TestProjects"));
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
                    OutputPath = @"/tmp/UnitTests"
                },

                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true
                }
            };
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, Serilog.Core.Logger.None);
            AnalyzerResult result = await analyzer.AnalyzeProject(projectPath);
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
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, Serilog.Core.Logger.None);
            AnalyzerResult result = (await analyzer.AnalyzeSolution(solutionPath)).FirstOrDefault();
            Assert.True(result != null);

            //Project has 16 nuget references and 19 framework/dll references:
            Assert.AreEqual(result.ProjectResult.ExternalReferences.NugetReferences.Count, 16);
            Assert.AreEqual(result.ProjectResult.ExternalReferences.SdkReferences.Count, 19);

            Assert.AreEqual(result.ProjectResult.SourceFiles.Count, 10);

            var houseController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HouseController.cs")).FirstOrDefault();
            Assert.NotNull(houseController);

            var classDeclarations = houseController.Children.OfType<AwsCodeAnalyzer.Model.NamespaceDeclaration>().FirstOrDefault();
            Assert.Greater(classDeclarations.Children.Count, 0);

            var classDeclaration = houseController.Children.OfType<AwsCodeAnalyzer.Model.NamespaceDeclaration>().FirstOrDefault().Children[0];
            Assert.NotNull(classDeclaration);

            var declarationNodes = classDeclaration.Children.OfType<AwsCodeAnalyzer.Model.DeclarationNode>();
            var attributeNodes = classDeclaration.Children.OfType<AwsCodeAnalyzer.Model.Annotation>();
            var methodDeclarations = classDeclaration.Children.OfType<Model.MethodDeclaration>();

            //HouseController has 20 identifiers declared within the class declaration:
            Assert.AreEqual(declarationNodes.Count(), 20);

            //HouseController has 17 attributes:
            Assert.AreEqual(attributeNodes.Count(), 17);

            //It has 6 method declarations
            Assert.AreEqual(methodDeclarations.Count(), 6);
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
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, Serilog.Core.Logger.None);
            var result = (await analyzer.AnalyzeSolution(solutionPath)).FirstOrDefault();
            Assert.True(result != null);

            Assert.AreEqual(result.ProjectResult.SourceFiles.Count, 20);

            //Project has 16 nuget references and 19 framework/dll references:
            Assert.AreEqual(result.ProjectResult.ExternalReferences.NugetReferences.Count, 10);
            Assert.AreEqual(result.ProjectResult.ExternalReferences.SdkReferences.Count, 21);

            var homeController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HomeController.cs")).FirstOrDefault();
            Assert.NotNull(homeController);

            var classDeclarations = homeController.Children.OfType<AwsCodeAnalyzer.Model.NamespaceDeclaration>().FirstOrDefault();
            Assert.Greater(classDeclarations.Children.Count, 0);

            var classDeclaration = homeController.Children.OfType<AwsCodeAnalyzer.Model.NamespaceDeclaration>().FirstOrDefault().Children[0];
            Assert.NotNull(classDeclaration);

            var declarationNodes = classDeclaration.Children.OfType<AwsCodeAnalyzer.Model.DeclarationNode>();
            var methodDeclarations = classDeclaration.Children.OfType<Model.MethodDeclaration>();

            //HouseController has 3 identifiers declared within the class declaration:
            Assert.AreEqual(declarationNodes.Count(), 3);

            //It has 2 method declarations
            Assert.AreEqual(methodDeclarations.Count(), 2);
        }

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(GetTstPath(@"Projects\Temp"), true);
        }
    }
}