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
        public async Task TestOwinParadise()
        {
            string solutionPath = CopySolutionFolderToTemp("OwinParadise.sln", _downloadsDir, _tempDir);

            FileAssert.Exists(solutionPath);

            var configuration = new AnalyzerConfiguration(LanguageOptions.CSharp);
            SetupDefaultAnalyzerConfiguration(configuration);

            var codeAnalyzerByLanguage = new CodeAnalyzerByLanguage(configuration, NullLogger.Instance);
            var getCodeAnalyzerByLanguageResults = codeAnalyzerByLanguage.Analyze(solutionPath);

            var codeAnalyzer = new Analyzers.CodeAnalyzer(configuration, NullLogger.Instance);
            var solution = await GetWorkspaceSolution(solutionPath);
            var results = await codeAnalyzer.Analyze(solution);
            var result = results.FirstOrDefault();
            Assert.True(result != null);
            Assert.False(result.ProjectBuildResult.IsSyntaxAnalysis);
            Assert.AreEqual(31, result.ProjectResult.ExternalReferences.NugetReferences.Count);
            
            // todo fix this. there should be sdk references
            //Assert.AreEqual(14, result.ProjectResult.ExternalReferences.SdkReferences.Count);
            Assert.AreEqual(14, result.ProjectResult.SourceFiles.Count);

            var owinExtraApi = result.ProjectResult.SourceFileResults.FirstOrDefault(
                    f => f.FilePath.EndsWith("OwinExtraApi.cs"));
            Assert.NotNull(owinExtraApi);

            var blockStatements = owinExtraApi.AllBlockStatements();
            var classDeclarations = owinExtraApi.AllClasses();
            var expressionStatements = owinExtraApi.AllExpressions();
            var invocationExpressions = owinExtraApi.AllInvocationExpressions();
            var literalExpressions = owinExtraApi.AllLiterals();
            var methodDeclarations = owinExtraApi.AllMethods();
            var constructorDeclarations = owinExtraApi.AllConstructors();
            var returnStatements = owinExtraApi.AllReturnStatements();
            var annotations = owinExtraApi.AllAnnotations();
            var namespaceDeclarations = owinExtraApi.AllNamespaces();
            var objectCreationExpressions = owinExtraApi.AllObjectCreationExpressions();
            var usingDirectives = owinExtraApi.AllUsingDirectives();
            var arguments = owinExtraApi.AllArguments();
            var memberAccess = owinExtraApi.AllMemberAccessExpressions();

            Assert.AreEqual(2, blockStatements.Count);
            Assert.AreEqual(1, classDeclarations.Count);
            Assert.AreEqual(19, expressionStatements.Count);
            Assert.AreEqual(14, invocationExpressions.Count);
            Assert.AreEqual(5, literalExpressions.Count);
            Assert.AreEqual(2, methodDeclarations.Count);
            Assert.AreEqual(0, returnStatements.Count);
            Assert.AreEqual(0, annotations.Count);
            Assert.AreEqual(1, namespaceDeclarations.Count);
            Assert.AreEqual(8, objectCreationExpressions.Count);
            Assert.AreEqual(10, usingDirectives.Count);
            Assert.AreEqual(14, arguments.Count);
            Assert.AreEqual(6, memberAccess.Count);

            var semanticMethodSignatures = methodDeclarations.Select(m => m.SemanticSignature);
            Assert.True(semanticMethodSignatures.Any(methodSignature => string.Compare(
                "public PortingParadise.OwinExtraApi.OwinAuthorization(IAuthorizationRequirement)",
                methodSignature,
                StringComparison.InvariantCulture) == 0));

            var houseControllerClass = classDeclarations.First(c => c.Identifier == "OwinExtraApi");
            Assert.AreEqual("public", houseControllerClass.Modifiers);

            var oldResults = await getCodeAnalyzerByLanguageResults;
            var oldResult = results.FirstOrDefault();
            Assert.IsNotNull(oldResult);

            VerifyWorkspaceResults(oldResult, result, "OwinExtraApi.cs");

            var languageAnalyzer = codeAnalyzerByLanguage.GetLanguageAnalyzerByFileType(".cs");
            var fileResult = languageAnalyzer.AnalyzeFile(
                owinExtraApi.FilePath,
                results.First().ProjectBuildResult,
                results.First());
            Assert.IsNotNull(fileResult);
        }

        private bool VerifyWorkspaceResults(
            AnalyzerResult buildalyzerResult,
            AnalyzerResult workspaceResult,
            string fileName)
        {
            Assert.AreEqual(buildalyzerResult.ProjectResult.ExternalReferences.NugetReferences.Count,
                workspaceResult.ProjectResult.ExternalReferences.NugetReferences.Count);
            Assert.AreEqual(buildalyzerResult.ProjectResult.ExternalReferences.SdkReferences.Count
                , workspaceResult.ProjectResult.ExternalReferences.SdkReferences.Count);
            Assert.AreEqual(buildalyzerResult.ProjectResult.SourceFiles.Count, 
                workspaceResult.ProjectResult.SourceFiles.Count);

            var buildalyzerSourceFileResult =
                buildalyzerResult.ProjectResult.SourceFileResults.FirstOrDefault(f =>
                    f.FilePath.EndsWith(fileName));
            var workspaceSourceFileResult =
                workspaceResult.ProjectResult.SourceFileResults.FirstOrDefault(f => 
                    f.FilePath.EndsWith(fileName));

            Assert.NotNull(buildalyzerSourceFileResult);
            Assert.NotNull(workspaceSourceFileResult);

            Assert.AreEqual(buildalyzerSourceFileResult.AllBlockStatements(),
                workspaceSourceFileResult.AllBlockStatements());
            Assert.AreEqual(buildalyzerSourceFileResult.AllClasses(),
                workspaceSourceFileResult.AllClasses());
            Assert.AreEqual(buildalyzerSourceFileResult.AllExpressions(),
                workspaceSourceFileResult.AllExpressions());
            Assert.AreEqual(buildalyzerSourceFileResult.AllInvocationExpressions(),
                workspaceSourceFileResult.AllInvocationExpressions());
            Assert.AreEqual(buildalyzerSourceFileResult.AllLiterals(),
                workspaceSourceFileResult.AllLiterals());
            Assert.AreEqual(buildalyzerSourceFileResult.AllMethods(),
                workspaceSourceFileResult.AllMethods());
            Assert.AreEqual(buildalyzerSourceFileResult.AllObjectCreationExpressions(),
                workspaceSourceFileResult.AllObjectCreationExpressions());
            Assert.AreEqual(buildalyzerSourceFileResult.AllUsingDirectives(),
                workspaceSourceFileResult.AllUsingDirectives());
            Assert.AreEqual(buildalyzerSourceFileResult.AllMemberAccessExpressions(),
                workspaceSourceFileResult.AllMemberAccessExpressions());
            return true;
        }

    }
}
