using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Assert;

namespace Codelyzer.Analysis.Tests
{
    //Implementations

    [TestFixture]
    [NonParallelizable]
    public class AwsAnalyzerTests : AwsBaseTest
    {
        public string tempDir = "";

        [SetUp]
        public void Setup()
        {
            Setup(GetType());
            tempDir = GetTstPath(Path.Combine(Constants.TempProjectDirectories));
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

            DownloadFromGitHub(@"https://github.com/FabianGosebrink/ASPNET-WebAPI-Sample/archive/671a629cab0382ecd6dec4833b3868f96f89da50.zip", "ASPNET-WebAPI-Sample-671a629cab0382ecd6dec4833b3868f96f89da50");
            DownloadFromGitHub(@"https://github.com/Duikmeester/MvcMusicStore/archive/e274968f2827c04cfefbe6493f0a784473f83f80.zip", "MvcMusicStore-e274968f2827c04cfefbe6493f0a784473f83f80");
            DownloadFromGitHub(@"https://github.com/nopSolutions/nopCommerce/archive/73567858b3e3ef281d1433d7ac79295ebed47ee6.zip", "nopCommerce-73567858b3e3ef281d1433d7ac79295ebed47ee6");

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
        public void TestCli()
        {
            string mvcMusicStorePath = Directory.EnumerateFiles(tempDir, "MvcMusicStore.sln", SearchOption.AllDirectories).FirstOrDefault();
            string[] args = { "-p", mvcMusicStorePath };
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
            string projectPath = string.Concat(GetTstPath(Path.Combine(new[] { "Projects", "CodelyzerDummy", "CodelyzerDummy" })), ".csproj");

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
                    ReferenceData = true,
                    InterfaceDeclarations = true,
                    GenerateBinFiles = true,
                    LoadBuildData = true,
                    ReturnStatements = true,
                    InvocationArguments = true
                }
            };
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, NullLogger.Instance);
            AnalyzerResult result = (await analyzer.AnalyzeSolution(solutionPath)).FirstOrDefault();
            Assert.True(result != null);
            Assert.False(result.ProjectBuildResult.IsSyntaxAnalysis);

            //Project has 16 nuget references and 19 framework/dll references:
            Assert.AreEqual(16, result.ProjectResult.ExternalReferences.NugetReferences.Count);
            Assert.AreEqual(19, result.ProjectResult.ExternalReferences.SdkReferences.Count);

            Assert.AreEqual(10, result.ProjectResult.SourceFiles.Count);

            var houseController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HouseController.cs")).FirstOrDefault();
            Assert.NotNull(houseController);

            var ihouseRepository = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("IHouseRepository.cs")).FirstOrDefault();
            Assert.NotNull(ihouseRepository);

            var blockStatements = houseController.AllBlockStatements();
            var classDeclarations = houseController.AllClasses();
            var expressionStatements = houseController.AllExpressions();
            var invocationExpressions = houseController.AllInvocationExpressions();
            var literalExpressions = houseController.AllLiterals();
            var methodDeclarations = houseController.AllMethods();
            var returnStatements = houseController.AllReturnStatements();
            var annotations = houseController.AllAnnotations();
            var namespaceDeclarations = houseController.AllNamespaces();
            var objectCreationExpressions = houseController.AllObjectCreationExpressions();
            var usingDirectives = houseController.AllUsingDirectives();
            var interfaces = ihouseRepository.AllInterfaces();
            var arguments = houseController.AllArguments();
            Assert.AreEqual(7, blockStatements.Count);
            Assert.AreEqual(1, classDeclarations.Count);
            Assert.AreEqual(89, expressionStatements.Count);
            Assert.AreEqual(75, invocationExpressions.Count);
            Assert.AreEqual(14, literalExpressions.Count);
            Assert.AreEqual(6, methodDeclarations.Count);
            Assert.AreEqual(16, returnStatements.Count);
            Assert.AreEqual(17, annotations.Count);
            Assert.AreEqual(1, namespaceDeclarations.Count);
            Assert.AreEqual(0, objectCreationExpressions.Count);
            Assert.AreEqual(10, usingDirectives.Count);
            Assert.AreEqual(1, interfaces.Count);
            Assert.AreEqual(63, arguments.Count);

            var dllFiles = Directory.EnumerateFiles(Path.Combine(result.ProjectResult.ProjectRootPath, "bin"), "*.dll");
            Assert.AreEqual(dllFiles.Count(), 16);

            await RunAgainWithChangedFile(solutionPath, result.ProjectBuildResult.ProjectPath, configuration, analyzer);
        }

        private async Task RunAgainWithChangedFile(string solutionPath, string projectPath, AnalyzerConfiguration configuration, CodeAnalyzer analyzer)
        {
            string projectFileContent = File.ReadAllText(projectPath);
            //Change the target to an invalid target to replicate an invalid msbuild installation
            File.WriteAllText(projectPath, projectFileContent.Replace(@"$(MSBuildBinPath)\Microsoft.CSharp.targets", @"InvalidTarget"));

            //Try without setting the flag, result should be null:
            AnalyzerResult result = (await analyzer.AnalyzeSolution(solutionPath)).FirstOrDefault();
            Assert.Null(result);

            //Try with setting the flag, syntax tree should be returned
            configuration.AnalyzeFailedProjects = true;
            result = (await analyzer.AnalyzeSolution(solutionPath)).FirstOrDefault();
            Assert.NotNull(result);
            Assert.True(result.ProjectBuildResult.IsSyntaxAnalysis);
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
                    ReferenceData = true,
                    LoadBuildData = true
                }
            };
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, NullLogger.Instance);
            using var result = (await analyzer.AnalyzeSolution(solutionPath)).FirstOrDefault();
            Assert.True(result != null);
            Assert.False(result.ProjectBuildResult.IsSyntaxAnalysis);

            Assert.AreEqual(28, result.ProjectResult.SourceFiles.Count);

            //Project has 16 nuget references and 19 framework/dll references:
            Assert.AreEqual(29, result.ProjectResult.ExternalReferences.NugetReferences.Count);
            Assert.AreEqual(24, result.ProjectResult.ExternalReferences.SdkReferences.Count);

            var homeController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HomeController.cs")).FirstOrDefault();
            Assert.NotNull(homeController);

            var classDeclarations = homeController.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault();
            Assert.Greater(classDeclarations.Children.Count, 0);

            var classDeclaration = homeController.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault().Children[0];
            Assert.NotNull(classDeclaration);

            var declarationNodes = classDeclaration.Children.OfType<Codelyzer.Analysis.Model.DeclarationNode>();
            var methodDeclarations = classDeclaration.Children.OfType<Model.MethodDeclaration>();

            //HouseController has 3 identifiers declared within the class declaration:
            Assert.AreEqual(4, declarationNodes.Count());

            //It has 2 method declarations
            Assert.AreEqual(2, methodDeclarations.Count());
        }

        [Test]
        public async Task TestAnalysis()
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
                    MethodInvocations = true,
                    Annotations = true,
                    LambdaMethods = true,
                    DeclarationNodes = true,
                    LocationData = true,
                    ReferenceData = true,
                    LoadBuildData = true
                }
            };
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, NullLogger.Instance);
            AnalyzerResult result = await analyzer.AnalyzeProject(projectPath);
            Assert.False(result.ProjectBuildResult.IsSyntaxAnalysis);

            Assert.True(result != null);

            // Extract the subject node
            var testClassRootNode = result.ProjectResult.SourceFileResults
                    .First(s => s.FileFullPath.EndsWith("Class2.cs"))
                as UstNode;

            // Nested class is found
            Assert.AreEqual(1, testClassRootNode.AllClasses().Count(c => c.Identifier == "NestedClass"));

            // Chained method is found
            Assert.AreEqual(1, testClassRootNode.AllInvocationExpressions().Count(c => c.MethodName == "ChainedMethod"));

            // Constructor is found
            Assert.AreEqual(1, testClassRootNode.AllConstructors().Count);
        }


        [Test]
        public async Task TestNopCommerce()
        {
            string solutionPath = Directory.EnumerateFiles(tempDir, "nopCommerce.sln", SearchOption.AllDirectories).FirstOrDefault();
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
                    ReferenceData = true,
                    EnumDeclarations = true,
                    StructDeclarations = true,
                    InterfaceDeclarations = true
                }
            };

            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, NullLogger.Instance);
            var results = (await analyzer.AnalyzeSolution(solutionPath)).ToList();

            var enumDeclarations = results.Sum(r => r.ProjectResult.SourceFileResults.Where(s => s.AllEnumDeclarations().Count > 0).Sum(s => s.AllEnumDeclarations().Count));
            var structDeclarations = results.Sum(r => r.ProjectResult.SourceFileResults.Where(s => s.AllStructDeclarations().Count > 0).Sum(s => s.AllStructDeclarations().Count));
            var arrowClauseStatements = results.Sum(r => r.ProjectResult.SourceFileResults.Where(s => s.AllArrowExpressionClauses().Count > 0).Sum(s => s.AllArrowExpressionClauses().Count));

            Assert.AreEqual(80, enumDeclarations);
            Assert.AreEqual(1, structDeclarations);
            Assert.AreEqual(2, arrowClauseStatements);

            results.ForEach(r => r.Dispose());
        }

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(GetTstPath(Path.Combine("Projects", "Temp")), true);
        }
    }
}
