using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
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
                    InvocationArguments = true,
                    ElementAccess = true,
                    MemberAccess = true
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
                    LoadBuildData = true,
                    ElementAccess = true,
                    MemberAccess = true
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
            var accountController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("AccountController.cs")).FirstOrDefault();
            var storeManagerController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("StoreManagerController.cs")).FirstOrDefault();

            Assert.NotNull(homeController);
            Assert.NotNull(accountController);
            Assert.NotNull(storeManagerController);

            var classDeclarations = homeController.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault();
            Assert.Greater(classDeclarations.Children.Count, 0);

            var accountClassDeclaration = accountController.Children.OfType<NamespaceDeclaration>().FirstOrDefault();
            Assert.NotNull(accountClassDeclaration);

            var classDeclaration = homeController.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault().Children[0];
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
            Assert.AreEqual("Roles",authorizeAttributeArgument.ArgumentName);
            Assert.AreEqual("\"Administrator\"",authorizeAttributeArgument.ArgumentExpression);

            var actionNameAttribute = storeManagerController.AllAnnotations().First(a => a.Identifier == "ActionName");
            var actionNameAttributeArgument = actionNameAttribute.AllAttributeArguments().First();
            Assert.IsNull(actionNameAttributeArgument.ArgumentName);
            Assert.AreEqual("\"Delete\"", actionNameAttributeArgument.ArgumentExpression);

            await TestMvcMusicStoreIncrementalBuildWithAnalzyer(analyzer, result, accountController);
        }


        [Test]
        public async Task TestMvcMusicStoreWithReferences()
        {
            string solutionPath = Directory.EnumerateFiles(tempDir, "MvcMusicStore.sln", SearchOption.AllDirectories).FirstOrDefault();
            FileAssert.Exists(solutionPath);
            string projectPath = Directory.EnumerateFiles(Path.GetDirectoryName(solutionPath), "*.csproj", SearchOption.AllDirectories).FirstOrDefault();

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


            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, NullLogger.Instance);

            //We need to build initially to generate the references and get their info:
            var start1 = DateTime.Now;
            using var tempResult = (await analyzer.AnalyzeSolution(solutionPath)).FirstOrDefault();
            var end1 = DateTime.Now - start1;

            var references = tempResult.ProjectBuildResult.Project.MetadataReferences.Select(m => m.Display).ToList();
            var referencesInfo = new Dictionary<string, List<string>>();
            referencesInfo.Add(projectPath, references);

            var start = DateTime.Now;
            using var result = (await analyzer.AnalyzeSolution(solutionPath, new Dictionary<string, List<string>>(), referencesInfo)).FirstOrDefault();
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

            var classDeclaration = homeController.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault().Children[0];
            var classDeclarationOld = homeControllerOld.Children.OfType<Codelyzer.Analysis.Model.NamespaceDeclaration>().FirstOrDefault().Children[0];
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

            await TestMvcMusicStoreIncrementalBuild(projectPath, references, analyzer, accountController);
        }

        private async Task TestMvcMusicStoreIncrementalBuildWithAnalzyer(CodeAnalyzer analyzer, AnalyzerResult result, RootUstNode accountController)
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

            result = await analyzer.AnalyzeFile(accountController.FileFullPath, result);
            var references = result.ProjectBuildResult.Project.MetadataReferences.Select(m => m.Display).ToList();
            var updatedSourcefile = result.ProjectResult.SourceFileResults.FirstOrDefault(s => s.FileFullPath.Contains("AccountController.cs"));

        }

        private async Task TestMvcMusicStoreIncrementalBuild(string projectPath, List<string> references, CodeAnalyzer analyzer, RootUstNode accountController)
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
            Assert.AreEqual(5, updatedSourceFile.AllDeclarationNodes().Count);
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
                    InterfaceDeclarations = true,
                    ElementAccess = true,
                    LambdaMethods = true,
                    InvocationArguments = true
                }
            };

            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, NullLogger.Instance);
            var results = (await analyzer.AnalyzeSolution(solutionPath)).ToList();

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

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(GetTstPath(Path.Combine("Projects", "Temp")), true);
        }
    }
}
