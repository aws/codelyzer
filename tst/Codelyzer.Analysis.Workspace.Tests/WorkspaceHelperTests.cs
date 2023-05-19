using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using NUnit.Framework;
using Codelyzer.Analysis.Workspace;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Codelyzer.Analysis.Workspace.Tests
{
    internal class WorkspaceHelperTests : WorkspaceBaseTest
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
            DownloadFromGitHub(@"https://github.com/marknfawaz/TestProjects/zipball/master/", "TestProjects-latest", _downloadsDir);
            SetupMsBuildLocator();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            DeleteDir(_tempDir);
            DeleteDir(_downloadsDir);
        }

        [Test]
        [TestCase("MixedClassLibrary.sln", ExpectedResult = true)]
        public async Task<bool> TestGetProjectBuildResults(string solutionName)
        {
            string solutionPath = CopySolutionFolderToTemp(solutionName, _downloadsDir, _tempDir);
            var solution = await GetWorkspaceSolution(solutionPath);
            var results = await new WorkspaceHelper(NullLogger.Instance).GetProjectBuildResults(solution);

            Assert.That(results.Count, Is.EqualTo(2));

            var vbProject = results[0];
            Assert.That(vbProject.SourceFileBuildResults.Count, Is.EqualTo(6));
            Assert.That(vbProject.ProjectGuid, Is.EqualTo("b72fbf90-e6d1-44a9-8cbd-d50a360f810c"));
            Assert.That(vbProject.ExternalReferences.NugetReferences.Count, Is.EqualTo(4));

            var csharpProject = results[1];
            Assert.That(csharpProject.SourceFileBuildResults.Count, Is.EqualTo(3));
            Assert.That(csharpProject.ProjectGuid, Is.EqualTo("93d5eb47-8ef4-4bd6-a4fc-adf81d92fb69"));
            Assert.That(csharpProject.ExternalReferences.NugetReferences.Count, Is.EqualTo(5));

            return true;
        }

        [Test]
        [TestCase("MixedClassLibrary.sln", ExpectedResult = true)]
        public async Task<bool> TestGetProjectBuildResultsGenerator(string solutionName)
        {
            var solutionPath = CopySolutionFolderToTemp(solutionName, _downloadsDir, _tempDir);
            var solution = await GetWorkspaceSolution(solutionPath);
            var generator = new WorkspaceHelper(NullLogger.Instance)
                .GetProjectBuildResultsGeneratorAsync(solution).GetAsyncEnumerator();
            var count = 0;
            while (await generator.MoveNextAsync())
            {
                var result = generator.Current;
                Assert.IsNotNull(result);
                count++;
            }
            Assert.That(count, Is.EqualTo(2));
            return true;
        }
    }
}
