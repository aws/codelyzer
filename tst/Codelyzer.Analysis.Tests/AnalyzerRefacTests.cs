using Codelyzer.Analysis.Analyzer;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Assert;

namespace Codelyzer.Analysis.Tests
{
    //Implementations

    [TestFixture]
    [NonParallelizable]
    public class AnalyzerRefacTests : AwsBaseTest
    {
        public string downloadsDir = ""; // A place to download example solutions one time for all tests
        public string tempDir = ""; // A place to copy example solutions for each test (as needed)

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Setup(GetType());
            tempDir = GetTstPath(Path.Combine(Constants.TempProjectDirectories));
            downloadsDir = GetTstPath(Path.Combine(Constants.TempProjectDownloadDirectories));
            DeleteDir(tempDir);
            DeleteDir(downloadsDir);
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(downloadsDir);
            DownloadTestProjects();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            DeleteDir(tempDir);
            DeleteDir(downloadsDir);
        }

        private void DownloadTestProjects()
        {
            DownloadFromGitHub(@"https://github.com/FabianGosebrink/ASPNET-WebAPI-Sample/archive/671a629cab0382ecd6dec4833b3868f96f89da50.zip", "ASPNET-WebAPI-Sample-671a629cab0382ecd6dec4833b3868f96f89da50");
            DownloadFromGitHub(@"https://github.com/Duikmeester/MvcMusicStore/archive/e274968f2827c04cfefbe6493f0a784473f83f80.zip", "MvcMusicStore-e274968f2827c04cfefbe6493f0a784473f83f80");
            DownloadFromGitHub(@"https://github.com/nopSolutions/nopCommerce/archive/73567858b3e3ef281d1433d7ac79295ebed47ee6.zip", "nopCommerce-73567858b3e3ef281d1433d7ac79295ebed47ee6");
            DownloadFromGitHub(@"https://github.com/marknfawaz/TestProjects/zipball/master/", "TestProjects-latest");
        }

        private void DownloadFromGitHub(string link, string name)
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

        [Test]
        public void TestCli()
        {
            string mvcMusicStorePath = Directory.EnumerateFiles(downloadsDir, "MvcMusicStore.sln", SearchOption.AllDirectories).FirstOrDefault();
            string[] args = { "-p", mvcMusicStorePath };
            AnalyzerCLI cli = new AnalyzerCLI();
            cli.HandleCommand(args);
            Assert.NotNull(cli);
            Assert.NotNull(cli.FilePath);
            Assert.NotNull(cli.Project);
            Assert.NotNull(cli.Configuration);
        }

        [Test]
        public void TestCliCustomBuild()
        {
            var msbuildPath = "ANY_PATH";
            var arguments = "arg1|arg2|arg3";

            string mvcMusicStorePath = Directory.EnumerateFiles(downloadsDir, "MvcMusicStore.sln", SearchOption.AllDirectories).FirstOrDefault();
            string[] args = { "-p", mvcMusicStorePath, "-x", msbuildPath, "-a", arguments };
            AnalyzerCLI cli = new AnalyzerCLI();
            cli.HandleCommand(args);
            Assert.NotNull(cli);
            Assert.AreEqual(msbuildPath, cli.Configuration.BuildSettings.MSBuildPath);
            Assert.AreEqual(arguments, string.Join("|", cli.Configuration.BuildSettings.BuildArguments));
        }

        [Test, TestCaseSource(nameof(TestCliMetaDataSource))]
        public async Task TestCliForMetaDataStringsAsync(string mdArgument, int enumNumbers, int ifaceNumbers)
        {
            string projectPath = string.Concat(GetTstPath(Path.Combine(new[] { "Projects", "CodelyzerDummy", "CodelyzerDummy" })), ".csproj");
            string[] args = { "-p", projectPath, "-m", mdArgument };
            AnalyzerCLI cli = new AnalyzerCLI();
            cli.HandleCommand(args);
            Assert.NotNull(cli);
            Assert.NotNull(cli.FilePath);
            Assert.NotNull(cli.Project);
            Assert.NotNull(cli.Configuration);

            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(cli.Configuration, NullLogger.Instance);
            AnalyzerResult result = await analyzerByLanguage.AnalyzeProject(projectPath);
            Assert.True(result != null);
            var testClassRootNode = result.ProjectResult.SourceFileResults
                    .First(s => s.FileFullPath.EndsWith("Class2.cs"))
                as UstNode;
            Assert.AreEqual(enumNumbers, testClassRootNode.AllEnumDeclarations().Count);
            var ifaceTestNode = result.ProjectResult.SourceFileResults.First(s => s.FileFullPath.EndsWith("ITest.cs"));
            Assert.AreEqual(ifaceNumbers, ifaceTestNode.AllInterfaces().Count);
        }

        [Test, TestCaseSource(nameof(TestCliMetaDataSource))]
        public async Task VBTestCliForMetaDataStringsAsync(string mdArgument, int enumNumbers, int ifaceNumbers)
        {
            string projectPath = Directory.EnumerateFiles(downloadsDir, "VBConsoleApp.vbproj", SearchOption.AllDirectories).FirstOrDefault();
            string[] args = { "-p", projectPath, "-m", mdArgument };
            AnalyzerCLI cli = new AnalyzerCLI();
            cli.HandleCommand(args);
            Assert.NotNull(cli);
            Assert.NotNull(cli.FilePath);
            Assert.NotNull(cli.Project);
            Assert.NotNull(cli.Configuration);
            
            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(cli.Configuration, NullLogger.Instance);
            AnalyzerResult result = await analyzerByLanguage.AnalyzeProject(projectPath);
            Assert.True(result != null);
            var testClassRootNode = result.ProjectResult.SourceFileResults
                    .First(s => s.FileFullPath.EndsWith("Class2.vb"))
                as UstNode;
            Assert.AreEqual(enumNumbers, testClassRootNode.AllEnumBlocks().Count);
            var ifaceTestNode = result.ProjectResult.SourceFileResults.First(s => s.FileFullPath.EndsWith("ITest.vb"));
            Assert.AreEqual(ifaceNumbers, ifaceTestNode.AllInterfaceBlocks().Count);
        }

        // netcoreapp3.1 project
        [TestCase("CoreWebApi.sln")]
        // net472 project with project references in csproj file
        [TestCase(@"NetFrameworkWithProjectReferences.sln")]
        public async Task TestAnalyzer_Builds_Projects_Successfully(string solutionFileName)
        {
            var solutionPath = CopySolutionFolderToTemp(solutionFileName);

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
            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var results = await analyzerByLanguage.AnalyzeSolution(solutionPath);
            var allBuildErrors = results.SelectMany(r => r.ProjectBuildResult.BuildErrors);

            CollectionAssert.IsNotEmpty(results);
            CollectionAssert.IsEmpty(allBuildErrors);
        }

        [TestCase(@"VBWebApi.sln")]
        public async Task VBTestAnalyzer_Builds_Projects_Successfully(string solutionFileName)
        {
            var solutionPath = CopySolutionFolderToTemp(solutionFileName);

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.Vb)
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
            

            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var results = await analyzerByLanguage.AnalyzeSolution(solutionPath);
            var allBuildErrors = results.SelectMany(r => r.ProjectBuildResult.BuildErrors);

            CollectionAssert.IsNotEmpty(results);
            CollectionAssert.IsEmpty(allBuildErrors);
        }
        private string CopySolutionFolderToTemp(string solutionName)
        {
            string solutionPath = Directory.EnumerateFiles(downloadsDir, solutionName, SearchOption.AllDirectories).FirstOrDefault();
            string solutionDir = Directory.GetParent(solutionPath).FullName;
            var newTempDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
            FileUtils.DirectoryCopy(solutionDir, newTempDir);

            solutionPath = Directory.EnumerateFiles(newTempDir, solutionName, SearchOption.AllDirectories).FirstOrDefault();
            return solutionPath;
        }

        [Test]
        public async Task TestSampleWebApi()
        {
            string solutionPath = CopySolutionFolderToTemp("SampleWebApi.sln");
            string solutionDir = Directory.GetParent(solutionPath).FullName;

            FileAssert.Exists(solutionPath);

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
                    InvocationArguments = true,
                    ElementAccess = true,
                    MemberAccess = true
                }
            };
            
            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var results = await analyzerByLanguage.AnalyzeSolution(solutionPath);
            AnalyzerResult result = results.FirstOrDefault();
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
            var constructorDeclarations = houseController.AllConstructors();
            var returnStatements = houseController.AllReturnStatements();
            var annotations = houseController.AllAnnotations();
            var namespaceDeclarations = houseController.AllNamespaces();
            var objectCreationExpressions = houseController.AllObjectCreationExpressions();
            var usingDirectives = houseController.AllUsingDirectives();
            var interfaces = ihouseRepository.AllInterfaces();
            var arguments = houseController.AllArguments();
            var memberAccess = houseController.AllMemberAccessExpressions();

            Assert.AreEqual(7, blockStatements.Count);
            Assert.AreEqual(1, classDeclarations.Count);
            Assert.AreEqual(62, expressionStatements.Count);
            Assert.AreEqual(41, invocationExpressions.Count);
            Assert.AreEqual(21, literalExpressions.Count);
            Assert.AreEqual(6, methodDeclarations.Count);
            Assert.AreEqual(16, returnStatements.Count);
            Assert.AreEqual(17, annotations.Count);
            Assert.AreEqual(1, namespaceDeclarations.Count);
            Assert.AreEqual(0, objectCreationExpressions.Count);
            Assert.AreEqual(10, usingDirectives.Count);
            Assert.AreEqual(1, interfaces.Count);
            Assert.AreEqual(34, arguments.Count);
            Assert.AreEqual(39, memberAccess.Count);

            var semanticMethodSignatures = methodDeclarations.Select(m => m.SemanticSignature);
            Assert.True(semanticMethodSignatures.Any(methodSignature => string.Compare(
                "public SampleWebApi.Controllers.HouseController.Create(SampleWebApi.Models.HouseDto)",
                methodSignature,
                StringComparison.InvariantCulture) == 0));

            var semanticConstructorSignatures = constructorDeclarations.Select(c => c.SemanticSignature);
            Assert.True(semanticConstructorSignatures.Any(constructorSignature => string.Compare(
                "public SampleWebApi.Controllers.HouseController.HouseController(SampleWebApi.Repositories.IHouseRepository, SampleWebApi.Services.IHouseMapper)",
                constructorSignature,
                StringComparison.InvariantCulture) == 0));

            var houseControllerClass = classDeclarations.First(c => c.Identifier == "HouseController");
            Assert.AreEqual("public", houseControllerClass.Modifiers);

            var houseMapper = result.ProjectResult.SourceFileResults.First(f => f.FilePath.EndsWith("HouseMapper.cs"));
            Assert.AreEqual(2, houseMapper.AllInvocationExpressions().Count);

            var dllFiles = Directory.EnumerateFiles(Path.Combine(result.ProjectResult.ProjectRootPath, "bin"), "*.dll");
            Assert.AreEqual(16, dllFiles.Count());


            await RunAgainWithChangedFile(solutionPath, result.ProjectBuildResult.ProjectPath, configuration, analyzerByLanguage);
        }

        [Test]
        public async Task VBTestSampleWebApi()
        {
            string solutionPath = CopySolutionFolderToTemp("VBWebApi.sln");
            string solutionDir = Directory.GetParent(solutionPath).FullName;

            FileAssert.Exists(solutionPath);

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.Vb)
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
                    InvocationArguments = true,
                    ElementAccess = true,
                    MemberAccess = true
                }
            };
            
            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var results = await analyzerByLanguage.AnalyzeSolution(solutionPath);
            AnalyzerResult result = results.FirstOrDefault();
            Assert.True(result != null);
            Assert.False(result.ProjectBuildResult.IsSyntaxAnalysis);

            //Project has 19 nuget references and 18 framework/dll references:
            Assert.Contains(result.ProjectResult.ExternalReferences.NugetReferences.Count, new int[] { 17, 20 });
            Assert.Contains(result.ProjectResult.ExternalReferences.SdkReferences.Count, new int[] { 18, 20 });

            Assert.AreEqual(40, result.ProjectResult.SourceFiles.Count);

            var helpController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HelpController.vb")).FirstOrDefault();
            Assert.NotNull(helpController);

            var iModelDocumentationProvider = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("IModelDocumentationProvider.vb")).FirstOrDefault();
            Assert.NotNull(iModelDocumentationProvider);

            var blockStatements = helpController.AllBlockStatements();
            var classBlocks = helpController.AllClassBlocks();
            var expressionStatements = helpController.AllExpressions();
            var invocationExpressions = helpController.AllInvocationExpressions();
            var literalExpressions = helpController.AllLiterals();
            var methodBlocks = helpController.AllMethodBlocks();
            var constructorBlocks = helpController.AllConstructorBlocks();
            var returnStatements = helpController.AllReturnStatements();
            var annotations = helpController.AllAttributeLists();
            var namespaceBlocks = helpController.AllNamespaceBlocks();
            var objectCreationExpressions = helpController.AllObjectCreationExpressions();
            var importStatements = helpController.AllImportsStatements();
            var interfaces = helpController.AllInterfaceBlocks();
            var argumentLists = helpController.AllArgumentLists();
            var memberAccess = helpController.AllMemberAccessExpressions();

            Assert.AreEqual(0, blockStatements.Count);
            Assert.AreEqual(1, classBlocks.Count);
            Assert.AreEqual(19, expressionStatements.Count);
            Assert.AreEqual(14, invocationExpressions.Count);
            Assert.AreEqual(4, literalExpressions.Count);
            Assert.AreEqual(3, methodBlocks.Count);
            Assert.AreEqual(6, returnStatements.Count);
            Assert.AreEqual(0, annotations.Count);
            Assert.AreEqual(1, namespaceBlocks.Count);
            Assert.AreEqual(0, objectCreationExpressions.Count);
            Assert.AreEqual(5, importStatements.Count);
            Assert.AreEqual(0, interfaces.Count);
            Assert.AreEqual(14, argumentLists.Count);
            Assert.AreEqual(13, memberAccess.Count);

            var ex = invocationExpressions.First(expression => 
                expression.SemanticOriginalDefinition == "System.Web.Mvc.Controller.View(String)");
            Assert.IsNotNull(ex);

            var semanticMethodSignatures = methodBlocks.Select(m => m.SemanticSignature);
            Assert.True(semanticMethodSignatures.Any(methodSignature => string.Compare(
                "Public Public Function Index() As System.Web.Mvc.ActionResult",
                methodSignature,
                StringComparison.InvariantCulture) == 0));

            var semanticConstructorSignatures = constructorBlocks.Select(c => c.SemanticSignature);
            Assert.True(semanticConstructorSignatures.Any(constructorSignature => string.Compare(
                "Public Public Sub New(config As System.Web.Http.HttpConfiguration)",
                constructorSignature,
                StringComparison.InvariantCulture) == 0));

            var helpControllerClass = classBlocks.First(c => c.Identifier == "HelpController");
            Assert.AreEqual("Public", helpControllerClass.Modifiers);

            var helpPageSampleKey = result.ProjectResult.SourceFileResults.First(f => f.FilePath.EndsWith("HelpPageSampleKey.vb"));
            Assert.AreEqual(26, helpPageSampleKey.AllInvocationExpressions().Count);

            var dllFiles = Directory.EnumerateFiles(Path.Combine(result.ProjectResult.ProjectRootPath, "bin"), "*.dll");
            Assert.AreEqual(16, dllFiles.Count());

            await RunAgainWithChangedFile(solutionPath, result.ProjectBuildResult.ProjectPath, configuration, analyzerByLanguage, "VisualBasic");
        }

        private async Task RunAgainWithChangedFile(string solutionPath, string projectPath, AnalyzerConfiguration configuration, CodeAnalyzerByLanguage analyzer, string language = "CSharp")
        {
            string projectFileContent = File.ReadAllText(projectPath);
            //Change the target to an invalid target to replicate an invalid msbuild installation
            File.WriteAllText(projectPath, projectFileContent.Replace($@"$(MSBuildBinPath)\Microsoft.{language}.targets", @"InvalidTarget"));

            //Try without setting the flag, result should be null:
            AnalyzerResult result = (await analyzer.AnalyzeSolution(solutionPath)).First();
            Assert.IsTrue(result.ProjectBuildResult.BuildErrors.Count > 0);

            //Try with setting the flag, syntax tree should be returned
            configuration.AnalyzeFailedProjects = true;
            result = (await analyzer.AnalyzeSolution(solutionPath)).FirstOrDefault();
            Assert.NotNull(result);
            Assert.True(result.ProjectBuildResult.IsSyntaxAnalysis);
        }

        [Test]
        public async Task TestMvcMusicStore()
        {
            string solutionPath = CopySolutionFolderToTemp("MvcMusicStore.sln");
            string solutionDir = Directory.GetParent(solutionPath).FullName;
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
                    LoadBuildData = true,
                    ElementAccess = true,
                    MemberAccess = true
                }
            };
            
            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            
            using var result = (await analyzerByLanguage.AnalyzeSolution(solutionPath)).FirstOrDefault(); 

             ValidateMvcMusicStoreResult(result);

            var accountController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("AccountController.cs")).FirstOrDefault();
            
            await TestMvcMusicStoreIncrementalBuildWithAnalyzer(analyzerByLanguage, result, accountController);
        }
        

        private void ValidateMvcMusicStoreResult(AnalyzerResult result)
        {
            Assert.True(result != null);
            Assert.False(result.ProjectBuildResult.IsSyntaxAnalysis);

            Assert.Contains(result.ProjectResult.SourceFiles.Count, new int[] { 28, 29 });

            Assert.Contains(result.ProjectResult.ExternalReferences.NugetReferences.Count, new int[] { 37, 29 });
            Assert.AreEqual(24, result.ProjectResult.ExternalReferences.SdkReferences.Count);

            var homeController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HomeController.cs")).FirstOrDefault();
            var accountController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("AccountController.cs")).FirstOrDefault();
            var storeManagerController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("StoreManagerController.cs")).FirstOrDefault();

            Assert.NotNull(homeController);
            Assert.NotNull(accountController);
            Assert.NotNull(storeManagerController);

            var classDeclarations = homeController.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault();
            Assert.Greater(classDeclarations.Children.Count, 0);

            var accountClassDeclaration = accountController.Children.OfType<NamespaceDeclaration>().FirstOrDefault();
            Assert.NotNull(accountClassDeclaration);

            var classDeclaration = homeController.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault().Children.OfType<Codelyzer.Analysis.Model.ClassDeclaration>().FirstOrDefault();
            Assert.NotNull(classDeclaration);

            var declarationNodes = classDeclaration.AllDeclarationNodes();
            var methodDeclarations = classDeclaration.AllMethods();

            var elementAccess = accountClassDeclaration.AllElementAccessExpressions();
            var memberAccess = accountClassDeclaration.AllMemberAccessExpressions();

            //HouseController has 3 identifiers declared within the class declaration:
            Assert.AreEqual(5, declarationNodes.Count());

            //It has 2 method declarations
            Assert.AreEqual(2, methodDeclarations.Count());

            Assert.AreEqual(2, elementAccess.Count());
            Assert.AreEqual(149, memberAccess.Count());

            foreach (var child in accountController.Children)
            {
                Assert.AreEqual(accountController, child.Parent);
            }

            var authorizeAttribute = storeManagerController.AllAnnotations().First(a => a.Identifier == "Authorize");
            var authorizeAttributeArgument = authorizeAttribute.AllAttributeArguments().First();
            Assert.AreEqual("Roles", authorizeAttributeArgument.ArgumentName);
            Assert.AreEqual("\"Administrator\"", authorizeAttributeArgument.ArgumentExpression);

            var actionNameAttribute = storeManagerController.AllAnnotations().First(a => a.Identifier == "ActionName");
            var actionNameAttributeArgument = actionNameAttribute.AllAttributeArguments().First();
            Assert.IsNull(actionNameAttributeArgument.ArgumentName);
            Assert.AreEqual("\"Delete\"", actionNameAttributeArgument.ArgumentExpression);
        }

        [Test]
        public async Task TestVBWebApiUsingGenerator()
        {
            string solutionPath = CopySolutionFolderToTemp("VBWebApi.sln");
            FileAssert.Exists(solutionPath);

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.Vb)
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
                    LoadBuildData = true,
                    ElementAccess = true,
                    MemberAccess = true

                }
            };
            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);

            var resultEnumerator = analyzerByLanguage.AnalyzeSolutionGeneratorAsync(solutionPath).GetAsyncEnumerator();

            if (await resultEnumerator.MoveNextAsync())
            {
                using var result = resultEnumerator.Current;

                ValidateVBWebApiResult(result);
            }
        }
        private void ValidateVBWebApiResult(AnalyzerResult result)
        {
            Assert.True(result != null);
            Assert.False(result.ProjectBuildResult.IsSyntaxAnalysis);

            Assert.AreEqual(40, result.ProjectResult.SourceFiles.Count);
            
            Assert.AreEqual(17, result.ProjectResult.ExternalReferences.NugetReferences.Count);
            Assert.AreEqual(18, result.ProjectResult.ExternalReferences.SdkReferences.Count);

            var helpController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HelpController.vb")).FirstOrDefault();
            var homeController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HomeController.vb")).FirstOrDefault();
            var valuesController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("ValuesController.vb")).FirstOrDefault();

            Assert.NotNull(helpController);
            Assert.NotNull(homeController);
            Assert.NotNull(valuesController);

            var classBlock = homeController.Children.OfType<Codelyzer.Analysis.Model.ClassBlock>().FirstOrDefault();
            Assert.Greater(classBlock.Children.Count, 1);

            var helpNamespaceBlock = helpController.Children.OfType<NamespaceBlock>().FirstOrDefault();
            Assert.NotNull(helpNamespaceBlock);

            var declarationNodes = classBlock.AllDeclarationNodes();
            var methodBlocks = classBlock.AllMethodBlocks();

            var elementAccess = helpNamespaceBlock.AllElementAccessExpressions();
            var memberAccess = helpNamespaceBlock.AllMemberAccessExpressions();

            Assert.AreEqual(5, declarationNodes.Count());

            Assert.AreEqual(1, methodBlocks.Count());

            Assert.AreEqual(0, elementAccess.Count());
            Assert.AreEqual(13, memberAccess.Count());

            foreach (var child in helpController.Children)
            {
                Assert.AreEqual(helpController, child.Parent);
            }
        }

        [Test]
        public async Task TestMvcMusicStoreWithReferences()
        {
            string solutionPath = CopySolutionFolderToTemp("MvcMusicStore.sln");
            string solutionDir = Directory.GetParent(solutionPath).FullName;

            FileAssert.Exists(solutionPath);
            string projectPath = FileUtils.GetProjectPathsFromSolutionFile(solutionPath).FirstOrDefault();

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
                    LoadBuildData = true,
                    ElementAccess = true,
                    MemberAccess = true
                }
            };
            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);

            //We need to build initially to generate the references and get their info:
            var start1 = DateTime.Now;
            using var tempResult = (await analyzerByLanguage.AnalyzeSolution(solutionPath)).FirstOrDefault();
            var end1 = DateTime.Now - start1;

            var references = tempResult.ProjectBuildResult.Project.MetadataReferences.Select(m => m.Display).ToList();
            var referencesInfo = new Dictionary<string, List<string>>();
            referencesInfo.Add(projectPath, references);

            var start = DateTime.Now;
            using var result = (await analyzerByLanguage.AnalyzeSolution(solutionPath, new Dictionary<string, List<string>>(), referencesInfo)).FirstOrDefault();
            var end = DateTime.Now - start;

            Assert.True(result != null);
            Assert.False(result.ProjectBuildResult.IsSyntaxAnalysis);

            Assert.AreEqual(28, result.ProjectResult.SourceFiles.Count);

            var homeController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HomeController.cs")).FirstOrDefault();
            var accountController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("AccountController.cs")).FirstOrDefault();
            var storeManagerController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("StoreManagerController.cs")).FirstOrDefault();

            var homeControllerOld = tempResult.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HomeController.cs")).FirstOrDefault();
            var accountControllerOld = tempResult.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("AccountController.cs")).FirstOrDefault();
            var storeManagerControllerOld = tempResult.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("StoreManagerController.cs")).FirstOrDefault();


            Assert.NotNull(homeController);
            Assert.NotNull(accountController);
            Assert.NotNull(storeManagerController);

            var classDeclarations = homeController.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault();
            var classDeclarationsOld = homeControllerOld.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault();
            Assert.Greater(classDeclarations.Children.Count, 0);

            var accountClassDeclaration = accountController.Children.OfType<NamespaceDeclaration>().FirstOrDefault();
            var accountClassDeclarationOld = accountControllerOld.Children.OfType<NamespaceDeclaration>().FirstOrDefault();
            Assert.NotNull(accountClassDeclaration);

            var classDeclaration = homeController.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault().Children.OfType<Codelyzer.Analysis.Model.ClassDeclaration>().FirstOrDefault();
            var classDeclarationOld = homeControllerOld.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault().Children.OfType<Codelyzer.Analysis.Model.ClassDeclaration>().FirstOrDefault();
            Assert.NotNull(classDeclaration);

            var declarationNodes = classDeclaration.AllDeclarationNodes();
            var methodDeclarations = classDeclaration.AllMethods();
            var elementAccess = accountClassDeclaration.AllElementAccessExpressions();
            var memberAccess = accountClassDeclaration.AllMemberAccessExpressions();

            var declarationNodesOld = classDeclarationOld.AllDeclarationNodes();
            var methodDeclarationsOld = classDeclarationOld.AllMethods();
            var elementAccessOld = accountClassDeclarationOld.AllElementAccessExpressions();
            var memberAccessOld = accountClassDeclarationOld.AllMemberAccessExpressions();

            //HouseController has 3 identifiers declared within the class declaration:
            Assert.AreEqual(declarationNodesOld.Count(), declarationNodes.Count());

            //It has 2 method declarations
            Assert.AreEqual(methodDeclarationsOld.Count(), methodDeclarations.Count());

            Assert.AreEqual(elementAccessOld.Count(), elementAccess.Count());
            Assert.AreEqual(memberAccessOld.Count(), memberAccess.Count());

            foreach (var child in accountController.Children)
            {
                Assert.AreEqual(accountController, child.Parent);
            }

            var authorizeAttribute = storeManagerController.AllAnnotations().First(a => a.Identifier == "Authorize");
            var authorizeAttributeArgument = authorizeAttribute.AllAttributeArguments().First();
            Assert.AreEqual("Roles", authorizeAttributeArgument.ArgumentName);
            Assert.AreEqual("\"Administrator\"", authorizeAttributeArgument.ArgumentExpression);

            var actionNameAttribute = storeManagerController.AllAnnotations().First(a => a.Identifier == "ActionName");
            var actionNameAttributeArgument = actionNameAttribute.AllAttributeArguments().First();
            Assert.IsNull(actionNameAttributeArgument.ArgumentName);
            Assert.AreEqual("\"Delete\"", actionNameAttributeArgument.ArgumentExpression);

            
            await TestMvcMusicStoreIncrementalBuild(projectPath, references, analyzerByLanguage, accountController);
        }

        private async Task TestMvcMusicStoreIncrementalBuildWithAnalyzer(CodeAnalyzerByLanguage analyzerByLanguage, AnalyzerResult result, RootUstNode accountController)
        {
            File.WriteAllText(accountController.FileFullPath, @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Mvc3ToolsUpdateWeb_Default.Models;
using MvcMusicStore.Models;

namespace Mvc3ToolsUpdateWeb_Default.Controllers
{
    public class AccountController : Controller
    {
        private void MigrateShoppingCart(string UserName)
        {
            // Associate shopping cart items with logged-in user
            var cart = ShoppingCart.GetCart(this.HttpContext);
            cart.MigrateCart(UserName);
            Session[ShoppingCart.CartSessionKey] = UserName;
        }

        
        // GET: /Account/ChangePassword
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword
        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                // ChangePassword will throw an exception rather
                // than return false in certain failure scenarios.
                bool changePasswordSucceeded;
                try
                {
                    MembershipUser currentUser = Membership.GetUser(User.Identity.Name, true /* userIsOnline */);
                    changePasswordSucceeded = currentUser.ChangePassword(model.OldPassword, model.NewPassword);
                }
                catch (Exception)
                {
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded)
                {
                    return RedirectToAction(""ChangePasswordSuccess"");
                }
                else
                {
                    ModelState.AddModelError("""", ""The current password is incorrect or the new password is invalid."");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
    }
}");
            var analyzer = analyzerByLanguage.GetLanguageAnalyzerByFileType(".cs");
            result = await analyzer.AnalyzeFile(accountController.FileFullPath, result);
            var references = result.ProjectBuildResult.Project.MetadataReferences.Select(m => m.Display).ToList();
            var updatedSourcefile = result.ProjectResult.SourceFileResults.FirstOrDefault(s => s.FileFullPath.Contains("AccountController.cs"));
        }

        private async Task TestMvcMusicStoreIncrementalBuild(string projectPath, List<string> references, CodeAnalyzerByLanguage analyzerByLanguage, RootUstNode accountController)
        {
            var filePath = accountController.FileFullPath;
            var fileContent = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Mvc3ToolsUpdateWeb_Default.Models;
using MvcMusicStore.Models;

namespace Mvc3ToolsUpdateWeb_Default.Controllers
{
    public class AccountController : Controller
    {
        private void MigrateShoppingCart(string UserName)
        {
            // Associate shopping cart items with logged-in user
            var cart = ShoppingCart.GetCart(this.HttpContext);
            cart.MigrateCart(UserName);
            Session[ShoppingCart.CartSessionKey] = UserName;
        }

        
        // GET: /Account/ChangePassword
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword
        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                // ChangePassword will throw an exception rather
                // than return false in certain failure scenarios.
                bool changePasswordSucceeded;
                try
                {
                    MembershipUser currentUser = Membership.GetUser(User.Identity.Name, true /* userIsOnline */);
                    changePasswordSucceeded = currentUser.ChangePassword(model.OldPassword, model.NewPassword);
                }
                catch (Exception)
                {
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded)
                {
                    return RedirectToAction(""ChangePasswordSuccess"");
                }
                else
                {
                    ModelState.AddModelError("""", ""The current password is incorrect or the new password is invalid."");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
    }
}";
            File.WriteAllText(accountController.FileFullPath, fileContent);

            var fileInfo = new Dictionary<string, string>();
            fileInfo.Add(filePath, fileContent);
            var projType = Path.GetExtension(projectPath).ToLower();
            var analyzer = analyzerByLanguage.GetLanguageAnalyzerByProjectType(projType);
            var oneFileResult = await analyzer.AnalyzeFile(projectPath, filePath, null, references);
            var listOfFilesResult = await analyzer.AnalyzeFile(projectPath, new List<string> { filePath }, null, references);
            var fileInfoResult = await analyzer.AnalyzeFile(projectPath, fileInfo, null, references);
            var oneFileWithContentResult = await analyzer.AnalyzeFile(projectPath, filePath, fileContent, null, references);

            var oneFileResultPre = await analyzer.AnalyzeFile(projectPath, filePath, references, null);
            var listOfFilesResultPre = await analyzer.AnalyzeFile(projectPath, new List<string> { filePath }, references, null);
            var fileInfoResultPre = await analyzer.AnalyzeFile(projectPath, fileInfo, references, null);
            var oneFileWithContentResultPre = await analyzer.AnalyzeFile(projectPath, filePath, fileContent, references, null);

            ValidateSourceFile(oneFileResult.RootNodes.FirstOrDefault());
            ValidateSourceFile(listOfFilesResult.RootNodes.FirstOrDefault());
            ValidateSourceFile(fileInfoResult.RootNodes.FirstOrDefault());
            ValidateSourceFile(oneFileWithContentResult.RootNodes.FirstOrDefault());

            ValidateSourceFile(oneFileResultPre.RootNodes.FirstOrDefault());
            ValidateSourceFile(listOfFilesResultPre.RootNodes.FirstOrDefault());
            ValidateSourceFile(fileInfoResultPre.RootNodes.FirstOrDefault());
            ValidateSourceFile(oneFileWithContentResultPre.RootNodes.FirstOrDefault());

            CommonUtils.RunGarbageCollection(null, "Test");
        }

        private void ValidateSourceFile(RootUstNode updatedSourceFile)
        {
            Assert.NotNull(updatedSourceFile);
            Assert.AreEqual(3, updatedSourceFile.AllMethods().Count);
            Assert.AreEqual(5, updatedSourceFile.AllLiterals().Count);
            Assert.AreEqual(24, updatedSourceFile.AllDeclarationNodes().Count);
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
            
            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            AnalyzerResult result = await analyzerByLanguage.AnalyzeProject(projectPath);
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
        public async Task VBTestAnalysis()
        {
            string projectPath = Directory.EnumerateFiles(downloadsDir, "VBConsoleApp.vbproj", SearchOption.AllDirectories).FirstOrDefault();

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.Vb)
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
            
            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            AnalyzerResult result = await analyzerByLanguage.AnalyzeProject(projectPath);
            Assert.False(result.ProjectBuildResult.IsSyntaxAnalysis);

            Assert.True(result != null);

            // Extract the subject node
            var testClassRootNode = result.ProjectResult.SourceFileResults
                    .First(s => s.FileFullPath.EndsWith("Class2.vb"))
                as UstNode;

            // Nested class is found
            Assert.AreEqual(1, testClassRootNode.AllClassBlocks().Count(c => c.Identifier == "NestedClass"));

            // Chained method is found
            Assert.AreEqual(1, testClassRootNode.AllInvocationExpressions().Count(c => c.MethodName == "ChainedMethod"));
            Assert.AreEqual("VBConsoleApp.Class2.ChainedMethod()",
                testClassRootNode.AllInvocationExpressions()
                    .First(c => c.MethodName == "ChainedMethod").SemanticOriginalDefinition);

            // Constructor is found
            Assert.AreEqual(1, testClassRootNode.AllConstructorBlocks().Count);
        }
        [Test]
        public async Task TestNopCommerce()
        {
            string solutionPath = Directory.EnumerateFiles(downloadsDir, "nopCommerce.sln", SearchOption.AllDirectories).FirstOrDefault();
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
                    InterfaceDeclarations = true,
                    ElementAccess = true,
                    LambdaMethods = true,
                    InvocationArguments = true
                }
            };

            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var results = (await analyzerByLanguage.AnalyzeSolution(solutionPath)).ToList();

            var enumDeclarations = results.Sum(r => r.ProjectResult.SourceFileResults.Where(s => s.AllEnumDeclarations().Count > 0).Sum(s => s.AllEnumDeclarations().Count));
            var structDeclarations = results.Sum(r => r.ProjectResult.SourceFileResults.Where(s => s.AllStructDeclarations().Count > 0).Sum(s => s.AllStructDeclarations().Count));
            var arrowClauseStatements = results.Sum(r => r.ProjectResult.SourceFileResults.Where(s => s.AllArrowExpressionClauses().Count > 0).Sum(s => s.AllArrowExpressionClauses().Count));
            var elementAccessStatements = results.Sum(r => r.ProjectResult.SourceFileResults.Where(s => s.AllElementAccessExpressions().Count > 0).Sum(s => s.AllElementAccessExpressions().Count));

            Assert.AreEqual(80, enumDeclarations);
            Assert.AreEqual(1, structDeclarations);
            Assert.AreEqual(1217, arrowClauseStatements);
            Assert.AreEqual(742, elementAccessStatements);

            var project = "Nop.Web";
            var file = @"Admin\Controllers\VendorController.cs";
            var webProject = results.First(r => r.ProjectResult.ProjectName == project);
            var simpleLambdas = webProject.ProjectResult.SourceFileResults.First(f => f.FilePath.EndsWith(file))
                .AllSimpleLambdaExpressions();
            var parenthesizedLambdas = webProject.ProjectResult.SourceFileResults.First(f => f.FilePath.EndsWith(file))
                .AllParenthesizedLambdaExpressions();
            var allLambdas = webProject.ProjectResult.SourceFileResults.First(f => f.FilePath.EndsWith(file))
                .AllLambdaExpressions();
            Assert.AreEqual(9, simpleLambdas.Count);
            Assert.AreEqual(0, parenthesizedLambdas.Count);
            Assert.AreEqual(9, allLambdas.Count);

            project = "Nop.Plugin.Tax.Avalara";
            file = "AvalaraTaxController.cs";
            webProject = results.First(r => r.ProjectResult.ProjectName == project);
            simpleLambdas = webProject.ProjectResult.SourceFileResults.First(f => f.FilePath.EndsWith(file))
                .AllSimpleLambdaExpressions();
            parenthesizedLambdas = webProject.ProjectResult.SourceFileResults.First(f => f.FilePath.EndsWith(file))
                .AllParenthesizedLambdaExpressions();
            allLambdas = webProject.ProjectResult.SourceFileResults.First(f => f.FilePath.EndsWith(file))
                .AllLambdaExpressions();
            Assert.AreEqual(5, simpleLambdas.Count);
            Assert.AreEqual(3, parenthesizedLambdas.Count);
            Assert.AreEqual(8, allLambdas.Count);

            project = "Nop.Services";
            file = "CustomerService.cs";
            webProject = results.First(r => r.ProjectResult.ProjectName == project);
            simpleLambdas = webProject.ProjectResult.SourceFileResults.First(f => f.FilePath.EndsWith(file))
                .AllSimpleLambdaExpressions();
            parenthesizedLambdas = webProject.ProjectResult.SourceFileResults.First(f => f.FilePath.EndsWith(file))
                .AllParenthesizedLambdaExpressions();
            allLambdas = webProject.ProjectResult.SourceFileResults.First(f => f.FilePath.EndsWith(file))
                .AllLambdaExpressions();
            var parenLambdasNoParameters = parenthesizedLambdas.Where(l => l.Parameters.Count == 0);
            var parenLambdas2Parameters = parenthesizedLambdas.Where(l => l.Parameters.Count == 2);
            Assert.AreEqual(88, simpleLambdas.Count);
            Assert.AreEqual(17, parenthesizedLambdas.Count);
            Assert.AreEqual(105, allLambdas.Count);
            Assert.AreEqual(8, parenLambdasNoParameters.Count());
            Assert.AreEqual(9, parenLambdas2Parameters.Count());
            results.ForEach(r => r.Dispose());
        }


        [Test]
        public async Task TestSampleNet60()
        {
            string solutionPath = Directory.EnumerateFiles(downloadsDir, "SampleNet60Mvc.sln", SearchOption.AllDirectories).FirstOrDefault();
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
                    InterfaceDeclarations = true,
                    ElementAccess = true,
                    LambdaMethods = true,
                    InvocationArguments = true
                }
            };

            
            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var results = (await analyzerByLanguage.AnalyzeSolution(solutionPath)).ToList();

            // Class with scoped namespace - should be able to get children
            var sampleClass = results.FirstOrDefault().ProjectResult.SourceFileResults.FirstOrDefault(x => x.FileFullPath.EndsWith("SampleClass.cs"));
            Assert.AreEqual(1, sampleClass.Children.Count);
        }

        [Test]
        public async Task TestBuildOnlyFramework_Successfully()
        {
            var solutionPath = CopySolutionFolderToTemp("BuildableWebApi.sln");

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
                },
                BuildSettings =
                {
                    BuildOnly = true,
                    BuildArguments = AnalyzerConfiguration.DefaultBuildArguments
                }
            };
            
            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            await analyzerByLanguage.AnalyzeSolution(solutionPath);

            //Check that the bin folder was created
            var binPath = Path.Join(Path.GetDirectoryName(solutionPath), "BuildableWebApi", "bin");
            Assert.IsTrue(Directory.Exists(binPath));

            //And it contains DLLs
            var dlls = Directory.EnumerateFiles(binPath, "*.dll", SearchOption.AllDirectories);
            Assert.AreEqual(84, dlls.Count());
            Assert.True(dlls.Any(c => c.Contains("BuildableWebApi.dll")));
        }

        [Test]
        public async Task VBTestBuildOnlyFramework_Successfully()
        {
            var solutionPath = CopySolutionFolderToTemp("VBWebApi.sln");

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.Vb)
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
                },
                BuildSettings =
                {
                    BuildOnly = true,
                    BuildArguments = AnalyzerConfiguration.DefaultBuildArguments
                }
            };
            

            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var results = (await analyzerByLanguage.AnalyzeSolution(solutionPath)).ToList();

            //Check that the bin folder was created
            var binPath = Path.Join(Path.GetDirectoryName(solutionPath), "VBWebApi", "bin");
            Assert.IsTrue(Directory.Exists(binPath));

            //And it contains DLLs
            var dlls = Directory.EnumerateFiles(binPath, "*.dll", SearchOption.AllDirectories);
            Assert.AreEqual(51, dlls.Count());
        }

        [Test]
        public async Task TestBuildOnlyCore_Successfully()
        {
            var solutionPath = CopySolutionFolderToTemp("CoreMVC.sln");

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
                },
                BuildSettings =
                {
                    BuildOnly = true,
                    BuildArguments = AnalyzerConfiguration.DefaultBuildArguments
                }
            };
            

            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            await analyzerByLanguage.AnalyzeSolution(solutionPath);

            //Check that the bin folder was created
            var binPath = Path.Join(Path.GetDirectoryName(solutionPath), "CoreMvc", "bin");
            Assert.IsTrue(Directory.Exists(binPath));

            //And it contains DLLs
            var dlls = Directory.EnumerateFiles(binPath, "*.dll", SearchOption.AllDirectories);
            Assert.AreEqual(2, dlls.Count());
        }

        [Test]
        public async Task VBTestBuildOnlyCore_Successfully()
        {
            var solutionPath = CopySolutionFolderToTemp("VBClassLibrary.sln");

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.Vb)
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
                },
                BuildSettings =
                {
                    BuildOnly = true,
                    BuildArguments = AnalyzerConfiguration.DefaultBuildArguments
                }
            };

            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            await analyzerByLanguage.AnalyzeSolution(solutionPath);

            //Check that the bin folder was created
            var binPath = Path.Join(Path.GetDirectoryName(solutionPath), "VBClassLibrary", "bin");
            Assert.IsTrue(Directory.Exists(binPath));

            //And it contains DLLs
            var dlls = Directory.EnumerateFiles(binPath, "*.dll", SearchOption.AllDirectories);
            Assert.AreEqual(1, dlls.Count());
        }
        [TestCase("SampleWebApi.sln")]
        [TestCase("MvcMusicStore.sln")]
        public async Task TestReferenceBuilds(string solutionName)
        {
            string solutionPath = CopySolutionFolderToTemp(solutionName);
            string solutionDir = Directory.GetParent(solutionPath).FullName;

            FileAssert.Exists(solutionPath);

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                ExportSettings =
                {
                    GenerateJsonOutput = false,
                    OutputPath = @"/tmp/UnitTests"
                },
                ConcurrentThreads = 1,
                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true,
                    Annotations = true,
                    DeclarationNodes = true,
                    LocationData = true,
                    ReferenceData = true,
                    EnumDeclarations = true,
                    StructDeclarations = true,
                    InterfaceDeclarations = true,
                    ElementAccess = true,
                    LambdaMethods = true,
                    InvocationArguments = true,
                    GenerateBinFiles = true,
                    LoadBuildData = true
                }
            };

            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var resultsUsingBuild = (await analyzerByLanguage.AnalyzeSolution(solutionPath)).ToList();

            var metaReferences = new Dictionary<string, List<string>>()
            {
                {
                    resultsUsingBuild.FirstOrDefault().ProjectBuildResult.ProjectPath,
                    resultsUsingBuild.FirstOrDefault().ProjectBuildResult.Project.MetadataReferences.Select(m => m.Display).ToList()
                }
            };

            //Files that are excluded from the list because they have more data in the reference analysis
            var exclusionsFile = Path.Combine(solutionDir, "Exclusions.txt");
            IEnumerable<string> exclusions = new List<string>();
            if (File.Exists(exclusionsFile))
            {
                exclusions = File.ReadAllLines(exclusionsFile).ToList().Select(l => l.Trim());
            }
            
            var results = (await analyzerByLanguage.AnalyzeSolution(solutionPath, null, metaReferences)).ToList();

            resultsUsingBuild.ForEach(resultUsingBuild => {
                var result = results.FirstOrDefault(r => r.ProjectResult.ProjectFilePath == resultUsingBuild.ProjectResult.ProjectFilePath);
                Assert.NotNull(result);
                var externalReferenceBuild = resultUsingBuild.ProjectResult.ExternalReferences;
                var externalReference = result.ProjectResult.ExternalReferences;
                Assert.True(externalReference.NugetReferences.SequenceEqual(externalReferenceBuild.NugetReferences));
                Assert.True(externalReference.NugetDependencies.SequenceEqual(externalReferenceBuild.NugetDependencies));
                Assert.True(externalReference.SdkReferences.SequenceEqual(externalReferenceBuild.SdkReferences));
                Assert.True(externalReference.ProjectReferences.SequenceEqual(externalReferenceBuild.ProjectReferences));
            });

            var sourceFiles = results.SelectMany(r => r.ProjectResult.SourceFileResults)
                .Where(s => !s.FileFullPath.Contains("AssemblyInfo.cs") &&
                !s.FileFullPath.Contains(".cshtml.g") &&
                !exclusions.Contains(Path.GetFileName(s.FileFullPath)));
            var sourceFilesUsingBuild = resultsUsingBuild.SelectMany(r => r.ProjectResult.SourceFileResults)
                .Where(s => !s.FileFullPath.Contains("AssemblyInfo.cs") && !exclusions.Contains(Path.GetFileName(s.FileFullPath)));

            sourceFiles.ToList().ForEach(sourceFile =>
            {
                var sourceFileUsingBuild = sourceFilesUsingBuild.FirstOrDefault(s => s.FileFullPath == sourceFile.FileFullPath);
                Assert.True(sourceFile.Equals(sourceFileUsingBuild));
            });
        }

        [Test]
        public async Task VBTestReferenceBuilds()
        {
            string solutionPath = CopySolutionFolderToTemp("VBWebApi.sln");
            string solutionDir = Directory.GetParent(solutionPath).FullName;

            FileAssert.Exists(solutionPath);

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.Vb)
            {
                ExportSettings =
                {
                    GenerateJsonOutput = false,
                    OutputPath = @"/tmp/UnitTests"
                },
                ConcurrentThreads = 1,
                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true,
                    Annotations = true,
                    DeclarationNodes = true,
                    LocationData = true,
                    ReferenceData = true,
                    EnumDeclarations = true,
                    StructDeclarations = true,
                    InterfaceDeclarations = true,
                    ElementAccess = true,
                    LambdaMethods = true,
                    InvocationArguments = true,
                    GenerateBinFiles = true,
                    LoadBuildData = true
                }
            };

            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var resultsUsingBuild = (await analyzerByLanguage.AnalyzeSolution(solutionPath)).ToList();
            var metaReferences = new Dictionary<string, List<string>>()
            {
                {
                    resultsUsingBuild.FirstOrDefault().ProjectBuildResult.ProjectPath,
                    resultsUsingBuild.FirstOrDefault().ProjectBuildResult.Project.MetadataReferences.Select(m => m.Display).ToList()
                }
            };

            //Files that are excluded from the list because they have more data in the reference analysis
            var exclusionsFile = Path.Combine(solutionDir, "Exclusions.txt");
            IEnumerable<string> exclusions = new List<string>();
            if (File.Exists(exclusionsFile))
            {
                exclusions = File.ReadAllLines(exclusionsFile).ToList().Select(l => l.Trim());
            }
            
            var results = (await analyzerByLanguage.AnalyzeSolution(solutionPath, null, metaReferences)).ToList();

            resultsUsingBuild.ForEach(resultUsingBuild => {
                var result = results.FirstOrDefault(r => r.ProjectResult.ProjectFilePath == resultUsingBuild.ProjectResult.ProjectFilePath);
                Assert.NotNull(result);
                var externalReferenceBuild = resultUsingBuild.ProjectResult.ExternalReferences;
                var externalReference = result.ProjectResult.ExternalReferences;
                Assert.True(externalReference.NugetReferences.SequenceEqual(externalReferenceBuild.NugetReferences));
                Assert.True(externalReference.NugetDependencies.SequenceEqual(externalReferenceBuild.NugetDependencies));
                Assert.True(externalReference.SdkReferences.SequenceEqual(externalReferenceBuild.SdkReferences));
                Assert.True(externalReference.ProjectReferences.SequenceEqual(externalReferenceBuild.ProjectReferences));
            });

            var sourceFiles = results.SelectMany(r => r.ProjectResult.SourceFileResults)
                .Where(s => !s.FileFullPath.Contains("AssemblyInfo.vb") &&
                !s.FileFullPath.Contains(".vbhtml.g") &&
                !exclusions.Contains(Path.GetFileName(s.FileFullPath)));
            var sourceFilesUsingBuild = resultsUsingBuild.SelectMany(r => r.ProjectResult.SourceFileResults)
                .Where(s => !s.FileFullPath.Contains("AssemblyInfo.vb") && !exclusions.Contains(Path.GetFileName(s.FileFullPath)));

            sourceFiles.ToList().ForEach(sourceFile =>
            {
                var sourceFileUsingBuild = sourceFilesUsingBuild.FirstOrDefault(s => s.FileFullPath == sourceFile.FileFullPath);

                if (sourceFile.FilePath != @"Areas\HelpPage\ModelDescriptions\ModelDescriptionGenerator.vb")
                {
                    Assert.True(sourceFile.Equals(sourceFileUsingBuild), $"sourceFile {sourceFile.FilePath} not equal to {sourceFileUsingBuild.FilePath} ");
                }
            });
        }

        [Test]
        public async Task TestModernizeGraph()
        {
            string solutionPath = CopySolutionFolderToTemp("Modernize.Web.sln");
            FileAssert.Exists(solutionPath);

            AnalyzerConfiguration configurationWithoutBuild = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                ExportSettings =
                {
                    GenerateJsonOutput = false,
                    OutputPath = @"/tmp/UnitTests"
                },
                ConcurrentThreads = 1,
                BuildSettings = {
                SyntaxOnly = true
                },
                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true,
                    Annotations = true,
                    DeclarationNodes = true,
                    LocationData = true,
                    ReferenceData = true,
                    EnumDeclarations = true,
                    StructDeclarations = true,
                    InterfaceDeclarations = true,
                    ElementAccess = true,
                    LambdaMethods = true,
                    InvocationArguments = true,
                    GenerateBinFiles = true,
                    LoadBuildData = true
                }
            };
            AnalyzerConfiguration configurationWithBuild = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                ExportSettings =
                {
                    GenerateJsonOutput = false,
                    OutputPath = @"/tmp/UnitTests"
                },
                ConcurrentThreads = 1,
                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true,
                    Annotations = true,
                    DeclarationNodes = true,
                    LocationData = true,
                    ReferenceData = true,
                    EnumDeclarations = true,
                    StructDeclarations = true,
                    InterfaceDeclarations = true,
                    ElementAccess = true,
                    LambdaMethods = true,
                    InvocationArguments = true,
                    GenerateBinFiles = true,
                    LoadBuildData = true
                }
            };

            
            CodeAnalyzerByLanguage analyzerWithoutBuild = new CodeAnalyzerByLanguage(configurationWithoutBuild, NullLogger.Instance);
            CodeAnalyzerByLanguage analyzerWithBuild = new CodeAnalyzerByLanguage(configurationWithBuild, NullLogger.Instance);

            var resultWithoutBuild = await analyzerWithoutBuild.AnalyzeSolutionWithGraph(solutionPath);
            var resultWithBuild = await analyzerWithBuild.AnalyzeSolutionWithGraph(solutionPath);

            var projectGraphWithoutBuild = resultWithoutBuild.CodeGraph.ProjectNodes;
            var projectGraphWithBuild = resultWithBuild.CodeGraph.ProjectNodes;
            var classGraphWithoutBuild = resultWithoutBuild.CodeGraph?.ClassNodes;
            var classGraphWithBuild = resultWithBuild.CodeGraph?.ClassNodes;

            // There are 5 projects in the solution
            Assert.AreEqual(5, projectGraphWithoutBuild.Count);
            Assert.AreEqual(5, projectGraphWithBuild.Count);

            //The Facade project has 3 Edges
            Assert.AreEqual(3, projectGraphWithoutBuild.FirstOrDefault(p => p.Name.Equals("Modernize.Web.Facade")).OutgoingEdges.Count);
            Assert.AreEqual(3, projectGraphWithBuild.FirstOrDefault(p => p.Name.Equals("Modernize.Web.Facade")).OutgoingEdges.Count);

            // The Mvc project has 3 Edges
            Assert.AreEqual(3, projectGraphWithoutBuild.FirstOrDefault(p => p.Name.Equals("Modernize.Web.Mvc")).OutgoingEdges.Count);

            // When project is prebuilt, connections are merged with dependent projects
            var mvcDependencies = projectGraphWithBuild.FirstOrDefault(p => p.Name.Equals("Modernize.Web.Mvc")).Edges.Count;
            Assert.True(mvcDependencies == 3 || mvcDependencies == 4);

            // The Models project has 0 Edges
            Assert.AreEqual(0, projectGraphWithoutBuild.FirstOrDefault(p => p.Name.Equals("Modernize.Web.Models")).OutgoingEdges.Count);
            Assert.AreEqual(0, projectGraphWithBuild.FirstOrDefault(p => p.Name.Equals("Modernize.Web.Models")).OutgoingEdges.Count);

            // There are 27 classes in the solution
            Assert.AreEqual(27, classGraphWithoutBuild.Count);
            Assert.AreEqual(27, classGraphWithBuild.Count);

            Assert.AreEqual(4, resultWithoutBuild.CodeGraph?.InterfaceNodes.Count);
            Assert.AreEqual(4, resultWithBuild.CodeGraph?.InterfaceNodes.Count);

            Assert.AreEqual(1, resultWithoutBuild.CodeGraph?.StructNodes.Count);
            Assert.AreEqual(1, resultWithBuild.CodeGraph?.StructNodes.Count);

            Assert.AreEqual(1, resultWithoutBuild.CodeGraph?.EnumNodes.Count);
            Assert.AreEqual(1, resultWithBuild.CodeGraph?.EnumNodes.Count);

            Assert.AreEqual(1, resultWithoutBuild.CodeGraph?.RecordNodes.Count);
            Assert.AreEqual(1, resultWithBuild.CodeGraph?.RecordNodes.Count);

            ValidateClassEdges(resultWithoutBuild.CodeGraph.ClassNodes);
            ValidateInterfaceEdges(resultWithoutBuild.CodeGraph.InterfaceNodes);
            ValidateMethodEdges(resultWithoutBuild.CodeGraph.MethodNodes);
            ValidateStructEdges(resultWithoutBuild.CodeGraph.StructNodes);
            ValidateEnumEdges(resultWithoutBuild.CodeGraph.EnumNodes);
            ValidateRecordEdges(resultWithoutBuild.CodeGraph.RecordNodes);

            ValidateClassEdges(resultWithBuild.CodeGraph.ClassNodes);
            ValidateInterfaceEdges(resultWithBuild.CodeGraph.InterfaceNodes);
            ValidateMethodEdges(resultWithBuild.CodeGraph.MethodNodes);
            ValidateStructEdges(resultWithBuild.CodeGraph.StructNodes);
            ValidateEnumEdges(resultWithBuild.CodeGraph.EnumNodes);
            ValidateRecordEdges(resultWithBuild.CodeGraph.RecordNodes);
        }

        private void ValidateClassEdges(HashSet<Node> nodes)
        {
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Common.Constants")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Models.Customer")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Models.Product")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Models.Purchase")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Models.Generics.ChildClass<ObjectType>")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Models.Generics.ParentClass<T>")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Models.Generics.ObjectType")).AllOutgoingEdges.Count);
            Assert.AreEqual(18, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider")).AllOutgoingEdges.Count);
            Assert.AreEqual(13, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.CustomerFacade")).AllOutgoingEdges.Count);
            Assert.AreEqual(9, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.Factory")).AllOutgoingEdges.Count);
            Assert.AreEqual(12, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ProductFacade")).AllOutgoingEdges.Count);
            Assert.AreEqual(15, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.PurchaseFacade")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.ExtractedClass")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.MvcApplication")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.SecondExtractedClass")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.BundleConfig")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.FilterConfig")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.RouteConfig")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.WebApiConfig")).AllOutgoingEdges.Count);
            Assert.AreEqual(16, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.CustomersController")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.HomeController")).AllOutgoingEdges.Count);
            Assert.AreEqual(8, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsAPIController")).AllOutgoingEdges.Count);
            Assert.AreEqual(16, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsController")).AllOutgoingEdges.Count);
            Assert.AreEqual(17, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.PurchasesController")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ValuesController")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Data.ModernizeWebMvcContext")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Data.ModernizeWebMvcContext")).AllOutgoingEdges.Count);
            Assert.AreEqual(7, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.UtilityClass")).AllOutgoingEdges.Count);
        }
        private void ValidateInterfaceEdges(HashSet<Node> nodes)
        {
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Models.Generics.IObjectType")).AllOutgoingEdges.Count);
            Assert.AreEqual(5, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ICustomerFacade")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.IProductFacade")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.IPurchaseFacade")).AllOutgoingEdges.Count);
        }
        private void ValidateStructEdges(HashSet<Node> nodes)
        {
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Models.ValuesStruct")).AllIncomingEdges.Count);
        }
        private void ValidateEnumEdges(HashSet<Node> nodes)
        {
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Models.ValuesEnum")).AllIncomingEdges.Count);
        }
        private void ValidateRecordEdges(HashSet<Node> nodes)
        {
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Models.ValuesRecord")).AllIncomingEdges.Count);
        }
        private void ValidateMethodEdges(HashSet<Node> nodes)
        {
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.GetSampleData()")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.GetProducts()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.GetProduct()")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.InsertProduct(Modernize.Web.Models.Product)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.DeleteProduct(Modernize.Web.Models.Product)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.UpdateProduct(Modernize.Web.Models.Product)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.GetCustomers()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.GetCustomer()")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.InsertCustomer(Modernize.Web.Models.Customer)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.DeleteCustomer(Modernize.Web.Models.Customer)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.UpdateCustomer(Modernize.Web.Models.Customer)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.GetPurchases()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.GetPurchase()")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.InsertPurchase(Modernize.Web.Models.Purchase)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.DeletePurchase(Modernize.Web.Models.Purchase)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Data.SqlProvider.UpdatePurchase(Modernize.Web.Models.Purchase)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.CustomerFacade.GetCustomers()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.CustomerFacade.InsertCustomer(Modernize.Web.Models.Customer)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.CustomerFacade.UpdateCustomer(Modernize.Web.Models.Customer)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.CustomerFacade.DeleteCustomer(Modernize.Web.Models.Customer)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.CustomerFacade.GetCustomer()")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.Factory.GetFacade<T>()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.Factory.CreateProductFacade()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.Factory.CreateCustomerFacade()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.Factory.CreatePurchaseFacade()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ProductFacade.GetProducts()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ProductFacade.InsertProduct(Modernize.Web.Models.Product)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ProductFacade.UpdateProduct(Modernize.Web.Models.Product)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ProductFacade.DeleteProduct(Modernize.Web.Models.Product)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ProductFacade.GetProduct()")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.PurchaseFacade.GetSampleData()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.PurchaseFacade.GetPurchases()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.PurchaseFacade.InsertPurchase(Modernize.Web.Models.Purchase)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.PurchaseFacade.UpdatePurchase(Modernize.Web.Models.Purchase)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.PurchaseFacade.DeletePurchase(Modernize.Web.Models.Purchase)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.PurchaseFacade.GetPurchase()")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ICustomerFacade.GetCustomers()")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ICustomerFacade.InsertCustomer(Modernize.Web.Models.Customer)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ICustomerFacade.UpdateCustomer(Modernize.Web.Models.Customer)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ICustomerFacade.DeleteCustomer(Modernize.Web.Models.Customer)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.ICustomerFacade.GetCustomer()")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Facade.IPurchaseFacade.GetSampleData()")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.MvcApplication.Application_Start()")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.CustomersController.Index()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.CustomersController.Details(int?)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.CustomersController.Create()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.CustomersController.Create(Modernize.Web.Models.Customer)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.CustomersController.Edit(int?)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.CustomersController.Edit(Modernize.Web.Models.Customer)")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.CustomersController.Delete(int?)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.CustomersController.DeleteConfirmed(int)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.CustomersController.Dispose(bool)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.HomeController.Index()")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.HomeController.About()")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.HomeController.Contact()")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsAPIController.GetProducts()")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsAPIController.GetProduct(int)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsAPIController.PutProduct(int, Modernize.Web.Models.Product)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsAPIController.PostProduct(Modernize.Web.Models.Product)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsAPIController.DeleteProduct(int)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsAPIController.Dispose(bool)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsAPIController.ProductExists(int)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsController.Index()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsController.Details(int?)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsController.Create()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsController.Create(Modernize.Web.Models.Product)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsController.Edit(int?)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsController.Edit(Modernize.Web.Models.Product)")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsController.Delete(int?)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsController.DeleteConfirmed(int)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ProductsController.Dispose(bool)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.PurchasesController.Index()")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.PurchasesController.Details(int?)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.PurchasesController.Create()")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.PurchasesController.Create(Modernize.Web.Models.Purchase)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.PurchasesController.Edit(int?)")).AllOutgoingEdges.Count);
            Assert.AreEqual(2, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.PurchasesController.Edit(Modernize.Web.Models.Purchase)")).AllOutgoingEdges.Count);
            Assert.AreEqual(3, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.PurchasesController.Delete(int?)")).AllOutgoingEdges.Count);
            Assert.AreEqual(1, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.PurchasesController.DeleteConfirmed(int)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.PurchasesController.Dispose(bool)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ValuesController.Get()")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ValuesController.Get(int)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ValuesController.Post(string)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ValuesController.Put(int, string)")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.Equals("Modernize.Web.Mvc.Controllers.ValuesController.Delete(int)")).AllOutgoingEdges.Count);

            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.StartsWith("Modernize.Web.Mvc.BundleConfig.RegisterBundles")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.StartsWith("Modernize.Web.Mvc.FilterConfig.RegisterGlobalFilters")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.StartsWith("Modernize.Web.Mvc.RouteConfig.RegisterRoutes")).AllOutgoingEdges.Count);
            Assert.AreEqual(0, nodes.FirstOrDefault(c => c.Identifier.StartsWith("Modernize.Web.Mvc.WebApiConfig.Register")).AllOutgoingEdges.Count);
        }

        [Test]
        public async Task VBLibraryClassAnalyze()
        {
            string solutionPath = CopySolutionFolderToTemp("VBClassLibrary.sln");
            string solutionDir = Directory.GetParent(solutionPath).FullName;

            FileAssert.Exists(solutionPath);

            var args = new[]
            {
                "-p", solutionPath
            };
            AnalyzerCLI cli = new AnalyzerCLI();
            cli.HandleCommand(args);
            cli.Configuration = new AnalyzerConfiguration(LanguageOptions.Vb)
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
                    LambdaMethods = true,
                    DeclarationNodes = true,
                    LocationData = true,
                    ReferenceData = true,
                    LoadBuildData = true,
                    ReturnStatements = true,
                    InterfaceDeclarations = true
                }
            };


            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(cli.Configuration, NullLogger.Instance);
            var results = await analyzerByLanguage.AnalyzeSolution(solutionPath);
            Assert.True(results != null);

        }

        [Test]
        public async Task VbOwinParadiseAnalyze()
        {
            string solutionPath = CopySolutionFolderToTemp("OwinParadiseVb.sln");
            string solutionDir = Directory.GetParent(solutionPath).FullName;

            FileAssert.Exists(solutionPath);

            var args = new[]
            {
                "-p", solutionPath
            };
            AnalyzerCLI cli = new AnalyzerCLI();
            cli.HandleCommand(args);
            cli.Configuration = new AnalyzerConfiguration(LanguageOptions.Vb)
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
                    LambdaMethods = true,
                    DeclarationNodes = true,
                    LocationData = true,
                    ReferenceData = true,
                    LoadBuildData = true,
                    ReturnStatements = true,
                    InterfaceDeclarations = true
                }
            };

            CodeAnalyzerByLanguage analyzerByLanguage = new CodeAnalyzerByLanguage(cli.Configuration, NullLogger.Instance);
            var results = await analyzerByLanguage.AnalyzeSolution(solutionPath);

            Assert.True(results != null);

            var testClassRootNode = results.First().ProjectResult.SourceFileResults
                    .First(s => s.FileFullPath.EndsWith("SignalR.vb"))
                as RootUstNode;

            var invocationExpressions = testClassRootNode.AllInvocationExpressions();
            var mapSignalMethod = invocationExpressions.FirstOrDefault(ex => ex.MethodName == "MapSignalR");
            Assert.IsNotNull(mapSignalMethod);
            Assert.AreEqual("Owin.IAppBuilder.MapSignalR()", mapSignalMethod.SemanticOriginalDefinition);

            await VbOwinParadiseAnalyzeIncrementalBuild(analyzerByLanguage, results.First(), testClassRootNode);
        }

        private async Task VbOwinParadiseAnalyzeIncrementalBuild(CodeAnalyzerByLanguage analyzerByLanguage, AnalyzerResult result, RootUstNode signalRNode)
        {
            File.WriteAllText(signalRNode.FileFullPath, @"Imports Microsoft.Owin.Hosting
Imports Owin
Imports Newtonsoft.Json

Namespace PortingParadise
    Public Class SignalR
        public class Startup
            public Sub Configuration(app As IAppBuilder)
                app.MapSignalR()
            End Sub

            public Sub Main(args  As String())
                Dim uri As String = ""http://localhost:9999/""
                Using (WebApp.Start(Of Startup)(uri))
                    Console.WriteLine(""Started"")
                    Process.Start(uri + ""signalr/negotiate"")
                    Console.ReadKey()
                End Using
            End Sub
        End Class
    End Class
End Namespace");

            var analyzer = analyzerByLanguage.GetLanguageAnalyzerByFileType(".vb");
            result = await analyzer.AnalyzeFile(signalRNode.FileFullPath, result);
            var references = result.ProjectBuildResult.Project.MetadataReferences.Select(m => m.Display).ToList();
            var updatedSourcefile = result.ProjectResult.SourceFileResults.FirstOrDefault(s => s.FileFullPath.Contains("SignalR.vb"));
            Assert.IsNotNull(updatedSourcefile);
            Assert.IsNotNull(references);
            Assert.Contains("Newtonsoft.Json", updatedSourcefile.AllImportsStatements().Select(s => s.Identifier).ToList());
        }

        [Test]
        public async Task VBConsoleAppWithCodeFactoryAnalyzer()
        {
            string solutionPath = CopySolutionFolderToTemp("VBConsoleApp.sln");

            FileAssert.Exists(solutionPath);
            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.Vb)
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
                    LambdaMethods = true,
                    DeclarationNodes = true,
                    LocationData = true,
                    ReferenceData = true,
                    LoadBuildData = true,
                    ReturnStatements = true,
                    InterfaceDeclarations = true
                }
            };
            var factory = CodeAnalyzerFactory.GetAnalyzer(configuration, NullLogger.Instance, solutionPath);

            List<AnalyzerResult> result = factory.AnalyzeSolution(solutionPath).Result;

            Assert.IsNotNull(result);
            var projectBuildResult = result.FirstOrDefault().ProjectBuildResult;
            Assert.IsNotNull(projectBuildResult);
            Assert.AreEqual(0, projectBuildResult.BuildErrors.Count);
            var sdkreferences = projectBuildResult.ExternalReferences.SdkReferences;
            var updatedSourcefile = result.FirstOrDefault().ProjectResult.SourceFileResults.FirstOrDefault();
            Assert.IsFalse(projectBuildResult.IsSyntaxAnalysis);
            Assert.IsNotNull(updatedSourcefile);
            Assert.IsNotNull(sdkreferences);
        }


        #region private methods
        private void DeleteDir(string path, int retries = 0)
        {
            if (retries <= 10)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }
                }
                catch (Exception)
                {
                    Thread.Sleep(10000);
                    DeleteDir(path, retries + 1);
                }
            }
        }

        private static IEnumerable<TestCaseData> TestCliMetaDataSource
        {
            get
            {
                yield return new TestCaseData("EnumDeclarations = true", 1, 0); // the space is deliberate
                yield return new TestCaseData("EnumDeclarations=true,InterfaceDeclarations=true", 1, 1);
                yield return new TestCaseData("EnumDeclarations=false,InterfaceDeclarations=true", 0, 1);
                yield return new TestCaseData("EnumDeclarations=true, InterfaceDeclarations=true", 1, 1);
                yield return new TestCaseData("InterfaceDeclarations=true", 0, 1);
                yield return new TestCaseData("", 0, 0);
            }

        }
        #endregion
    }
}