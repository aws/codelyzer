using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codelyzer.Analysis.Analyzer;
using Microsoft.Extensions.Logging.Abstractions;
using Codelyzer.Analysis.Model;
using Microsoft.Build.Locator;
using AnalyzerResult = Codelyzer.Analysis.Model.AnalyzerResult;

namespace Codelyzer.Analysis.Workspace.Tests
{
    internal class AnalyzerWithWorkspaceTests : WorkspaceBaseTest
    {
        private string _downloadsDir = ""; // A place to download example solutions one time for all tests
        private string _tempDir = ""; // A place to copy example solutions for each test (as needed)

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Setup(GetType());
            _tempDir = GetTstPath(Path.Combine(Constants.TempProjectDirectories));
            _downloadsDir = GetTstPath(Path.Combine(Constants.TempProjectDownloadDirectories));
            DeleteDir(_tempDir);
            DeleteDir(_downloadsDir);
            Directory.CreateDirectory(_tempDir);
            Directory.CreateDirectory(_downloadsDir);
            DownloadTestProjects();
            SetupMsBuildLocator();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            DeleteDir(_tempDir);
            DeleteDir(_downloadsDir);
        }

        private void DownloadTestProjects()
        {
            DownloadFromGitHub(@"https://github.com/marknfawaz/TestProjects/zipball/master/", "TestProjects-latest", _downloadsDir);
        }

        [Test]
        [TestCase("OwinParadise.sln", "OwinExtraApi.cs")]
        // TODO Team to check after release
        //[TestCase("CoreWebApi.sln", "WeatherForecastController.cs")]
        public async Task TestAnalyze(string solutionName, string fileName)
        {
            var (solutionPath, configuration, expectedResults) = CommonTestSetup(solutionName);

            var codeAnalyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var codeAnalyzer = new Analyzers.CodeAnalyzer(configuration, NullLogger.Instance);
            var solution = await GetWorkspaceSolution(solutionPath);
            var results = await codeAnalyzer.Analyze(solution);
            var getCodeAnalyzerByLanguageResults = codeAnalyzerByLanguage.Analyze(solutionPath);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);
            Assert.False(result.ProjectBuildResult.IsSyntaxAnalysis);
            Assert.That(result.ProjectResult.ExternalReferences.NugetReferences,
                Has.Count.EqualTo(expectedResults["NugetReferencesCount"]));
            
            Assert.That(result.ProjectResult.SourceFiles, Has.Count.EqualTo(expectedResults["SourceFilesCount"]));

            var fileUstNode = result.ProjectResult.SourceFileResults.FirstOrDefault(
                    f => f.FilePath.EndsWith(fileName));
            Assert.NotNull(fileUstNode);
            
            var classDeclarations = fileUstNode.AllClasses();
            var methodDeclarations = fileUstNode.AllMethods();
            
            VerifyFileUstNode(fileUstNode, expectedResults);

            var semanticMethodSignatures = methodDeclarations.Select(m => m.SemanticSignature);
            Assert.That(semanticMethodSignatures.Any(methodSignature => string.Compare(
                expectedResults["MethodSignature"].ToString(),
                methodSignature,
                StringComparison.InvariantCulture) == 0), Is.True);

            var houseControllerClass = classDeclarations.First(c =>
                c.Identifier == expectedResults["ClassDeclarationIdentifier"].ToString());
            Assert.That(houseControllerClass.Modifiers, Is.EqualTo(expectedResults["ClassDeclarationModifier"]));

            var oldResults = await getCodeAnalyzerByLanguageResults;
            var oldResult = oldResults.FirstOrDefault();
            Assert.That(oldResult, Is.Not.Null);
            VerifyWorkspaceResults(oldResult, result, fileName);

            var languageAnalyzer = codeAnalyzerByLanguage.GetLanguageAnalyzerByFileType(".cs");
            var fileResult = languageAnalyzer.AnalyzeFile(
                fileUstNode.FilePath,
                results.First().ProjectBuildResult,
                results.First());
            Assert.That(fileResult, Is.Not.Null);
        }

        [Test]
        [TestCase("OwinParadise.sln", "OwinExtraApi.cs")]
        // TODO Team to check after release
        //[TestCase("CoreWebApi.sln", "WeatherForecastController.cs")]
        public async Task TestAnalyzeGenerator(string solutionName, string fileName)
        {
            var (solutionPath, configuration, expectedResults) = CommonTestSetup(solutionName);
            
            var getSolutionTask = GetWorkspaceSolution(solutionPath);
            
            var codeAnalyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var codeAnalyzer = new Analyzers.CodeAnalyzer(configuration, NullLogger.Instance);
            
            var resultAsyncEnumerable = codeAnalyzer.AnalyzeGeneratorAsync(await getSolutionTask);
            var results = new List<AnalyzerResult>();
            var resultEnumerator = resultAsyncEnumerable.GetAsyncEnumerator();
            while (await resultEnumerator.MoveNextAsync())
            {
                results.Add(resultEnumerator.Current);
            }
            var codeAnalyzerByLanguageResults = await codeAnalyzerByLanguage.Analyze(solutionPath);
            VerifyWorkspaceResults(codeAnalyzerByLanguageResults.First(), results.First(), fileName);
        }

        private (string, AnalyzerConfiguration, Dictionary<string, object>) CommonTestSetup(string solutionName)
        {
            string solutionPath = CopySolutionFolderToTemp(solutionName, _downloadsDir, _tempDir);
            FileAssert.Exists(solutionPath);
            
            var expectedResults = ExpectedResults.GetExpectedAnalyzerResults(solutionName);
            var configuration = new AnalyzerConfiguration(LanguageOptions.CSharp);
            SetupDefaultAnalyzerConfiguration(configuration);
            
            return (solutionPath, configuration, expectedResults);
        }
        
        private void VerifyFileUstNode(RootUstNode fileUstNode, Dictionary<string, object> expectedResults)
        {
            Assert.That(fileUstNode.AllBlockStatements(), Has.Count.EqualTo(expectedResults["BlockStatementsCount"]));
            Assert.That(fileUstNode.AllClasses(), Has.Count.EqualTo(expectedResults["ClassesCount"]));
            Assert.That(fileUstNode.AllExpressions(), Has.Count.EqualTo(expectedResults["ExpressionsCount"]));
            Assert.That(fileUstNode.AllInvocationExpressions(),
                Has.Count.EqualTo(expectedResults["InvocationExpressionsCount"]));
            Assert.That(fileUstNode.AllLiterals(), Has.Count.EqualTo(expectedResults["LiteralExpressionsCount"]));
            Assert.That(fileUstNode.AllMethods(), Has.Count.EqualTo(expectedResults["MethodsCount"]));
            Assert.That(fileUstNode.AllReturnStatements(), Has.Count.EqualTo(expectedResults["ReturnStatementsCount"]));
            Assert.That(fileUstNode.AllAnnotations(), Has.Count.EqualTo(expectedResults["AnnotationsCount"]));
            Assert.That(fileUstNode.AllNamespaces(), Has.Count.EqualTo(expectedResults["NamespacesCount"]));
            Assert.That(fileUstNode.AllObjectCreationExpressions(),
                Has.Count.EqualTo(expectedResults["ObjectCreationCount"]));
            Assert.That(fileUstNode.AllUsingDirectives(), Has.Count.EqualTo(expectedResults["UsingDirectivesCount"]));
            Assert.That(fileUstNode.AllArguments(), Has.Count.EqualTo(expectedResults["ArgumentsCount"]));
            Assert.That(fileUstNode.AllMemberAccessExpressions(),
                Has.Count.EqualTo(expectedResults["MemberAccessExpressionsCount"]));
        }

        private void VerifyWorkspaceResults(
            AnalyzerResult buildalyzerResult,
            AnalyzerResult workspaceResult,
            string fileName)
        {
            Assert.That(workspaceResult.ProjectResult.ExternalReferences.NugetReferences.Count,
                Is.EqualTo(buildalyzerResult.ProjectResult.ExternalReferences.NugetReferences.Count));
            Assert.That(workspaceResult.ProjectResult.ExternalReferences.SdkReferences.Count,
                Is.EqualTo(buildalyzerResult.ProjectResult.ExternalReferences.SdkReferences.Count));
            Assert.That(workspaceResult.ProjectResult.SourceFiles.Count,
                Is.EqualTo(buildalyzerResult.ProjectResult.SourceFiles.Count));

            var buildalyzerSourceFileResult =
                buildalyzerResult.ProjectResult.SourceFileResults.FirstOrDefault(f =>
                    f.FilePath.EndsWith(fileName));
            var workspaceSourceFileResult =
                workspaceResult.ProjectResult.SourceFileResults.FirstOrDefault(f => 
                    f.FilePath.EndsWith(fileName));
            
            Assert.That(buildalyzerSourceFileResult, Is.Not.Null);
            Assert.That(workspaceSourceFileResult, Is.Not.Null);
            Assert.That(workspaceSourceFileResult.AllBlockStatements(),
                Is.EqualTo(buildalyzerSourceFileResult.AllBlockStatements()));
            Assert.That(workspaceSourceFileResult.AllClasses(), Is.EqualTo(buildalyzerSourceFileResult.AllClasses()));
            Assert.That(workspaceSourceFileResult.AllExpressions(),
                Is.EqualTo(buildalyzerSourceFileResult.AllExpressions()));
            Assert.That(workspaceSourceFileResult.AllInvocationExpressions(),
                Is.EqualTo(buildalyzerSourceFileResult.AllInvocationExpressions()));
            Assert.That(workspaceSourceFileResult.AllLiterals(), Is.EqualTo(buildalyzerSourceFileResult.AllLiterals()));
            Assert.That(workspaceSourceFileResult.AllMethods(), Is.EqualTo(buildalyzerSourceFileResult.AllMethods()));
            Assert.That(workspaceSourceFileResult.AllObjectCreationExpressions(),
                Is.EqualTo(buildalyzerSourceFileResult.AllObjectCreationExpressions()));
            Assert.That(workspaceSourceFileResult.AllUsingDirectives(),
                Is.EqualTo(buildalyzerSourceFileResult.AllUsingDirectives()));
            Assert.That(workspaceSourceFileResult.AllMemberAccessExpressions(),
                Is.EqualTo(buildalyzerSourceFileResult.AllMemberAccessExpressions()));
        }

    }
}
