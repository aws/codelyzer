using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Assert;
using Codelyzer.Analysis.Build;
using Moq;
using System.Runtime.InteropServices;

namespace Codelyzer.Analysis.Tests
{
    [TestFixture]
    public class WorkspaceBuilderHelperTest: AwsBaseTest
    {
        public string tempDir = "";
        public string testvs2022Path = "";
        public string testvs2019Path = "";
        public string testvs2017And19BuildToolsPath = "";
        public string testvs2022MissingTargetsPath = "";
        public string testMSBuild14Path = "";
        public string testMissingTargetsPath = "";
        
        MSBuildDetector msBuildDetector = new MSBuildDetector();
        Mock<ILogger> mockedLogger = new();
        WorkspaceBuilderHelper workspaceBuilderHelper;

        [OneTimeSetUp]
        public void Setup()
        {
            Setup(GetType());
            tempDir = GetTstPath(Path.Combine(Constants.TempProjectDirectories));
            SetupTestProject();
            workspaceBuilderHelper  = new WorkspaceBuilderHelper(mockedLogger.Object, "",
            new AnalyzerConfiguration(LanguageOptions.CSharp));
        }

        private void SetupTestProject() 
        {
            var tempDirectory = Directory.CreateDirectory(tempDir);
            var testProjectZipPath = GetTstPath(Path.Combine("TestProjects", "vs2022Test.zip"));

            using (ZipArchive archive = ZipFile.Open(
                testProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempDirectory.FullName);
            }
            testvs2022Path = Path.Combine(tempDirectory.FullName, "vs2022Test");

            
            testProjectZipPath = GetTstPath(Path.Combine("TestProjects", "vs2019Test.zip"));

            using (ZipArchive archive = ZipFile.Open(
                testProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempDirectory.FullName);
            }
            testvs2019Path = Path.Combine(tempDirectory.FullName, "vs2019Test");


            testProjectZipPath = GetTstPath(Path.Combine("TestProjects", "vs2017And19BuildToolsTest.zip"));

            using (ZipArchive archive = ZipFile.Open(
                testProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempDirectory.FullName);
            }
            testvs2017And19BuildToolsPath = Path.Combine(tempDirectory.FullName, "vs2017And19BuildToolsTest");


            testProjectZipPath = GetTstPath(Path.Combine("TestProjects", "vs2022MissingTargetsTest.zip"));

            using (ZipArchive archive = ZipFile.Open(
                testProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempDirectory.FullName);
            }
            testvs2022MissingTargetsPath = Path.Combine(tempDirectory.FullName, "vs2022MissingTargetsTest");


            testProjectZipPath = GetTstPath(Path.Combine("TestProjects", "MSBuild14Test.zip"));

            using (ZipArchive archive = ZipFile.Open(
                testProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempDirectory.FullName);
            }
            testMSBuild14Path = Path.Combine(tempDirectory.FullName, "MSBuild14Test");


            testProjectZipPath = GetTstPath(Path.Combine("TestProjects", "missingTargetsTest.zip"));

            using (ZipArchive archive = ZipFile.Open(
                testProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempDirectory.FullName);
            }
            testMissingTargetsPath = Path.Combine(tempDirectory.FullName, "missingTargetsTest");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(testvs2022Path, true);
            Directory.Delete(testvs2019Path, true);
            Directory.Delete(testvs2017And19BuildToolsPath, true);
            Directory.Delete(testvs2022MissingTargetsPath, true);
            Directory.Delete(testMSBuild14Path, true);
            Directory.Delete(testMissingTargetsPath, true);
        }

        [Test]
        public void TestVS2022()
        {
            string actualMsBuildPath = Path.Combine(testvs2022Path, Constants.vs2022MSBuildPath);
            string msbuildpath = msBuildDetector.GetFirstMatchingMsBuildFromPath(programFilesPath: Path.Combine(testvs2022Path, Constants.programFiles), programFilesX86Path: Path.Combine(testvs2022Path, Constants.programFilesx86));
            Assert.AreEqual(actualMsBuildPath,msbuildpath);
        }

        [Test]
        public void TestVS2019()
        {
            string actualMsBuildPath = Path.Combine(testvs2019Path, Constants.vs2019MSBuildPath);
            string msbuildpath = msBuildDetector.GetFirstMatchingMsBuildFromPath(programFilesPath: Path.Combine(testvs2019Path, Constants.programFiles), programFilesX86Path: Path.Combine(testvs2019Path, Constants.programFilesx86));
            Assert.AreEqual(actualMsBuildPath, msbuildpath);
        }

        [Test]
        public void TestVS2017And2019BuildTools()
        {
            string actualMsBuildPath = Path.Combine(testvs2017And19BuildToolsPath, Constants.vs2019BuildToolsMSBuildPath);
            string msbuildpath = msBuildDetector.GetFirstMatchingMsBuildFromPath(programFilesPath: Path.Combine(testvs2017And19BuildToolsPath, Constants.programFiles), programFilesX86Path: Path.Combine(testvs2017And19BuildToolsPath, Constants.programFilesx86));
            Assert.AreEqual(actualMsBuildPath, msbuildpath);
        }

        [Test]
        public void TestVS2022MissingTargets()
        {
            string actualMsBuildPath = Path.Combine(testvs2022MissingTargetsPath, Constants.vs2019MSBuildPath);
            string msbuildpath = msBuildDetector.GetFirstMatchingMsBuildFromPath(programFilesPath: Path.Combine(testvs2022MissingTargetsPath, Constants.programFiles), programFilesX86Path: Path.Combine(testvs2022MissingTargetsPath, Constants.programFilesx86));
            Assert.AreEqual(actualMsBuildPath, msbuildpath);
        }

        [Test]
        public void TestMSBuild14()
        {
            string actualMsBuildPath = Path.Combine(testMSBuild14Path, Constants.MSBuild14Path);
            string msbuildpath = msBuildDetector.GetFirstMatchingMsBuildFromPath(programFilesPath: Path.Combine(testMSBuild14Path, Constants.programFiles), programFilesX86Path: Path.Combine(testMSBuild14Path, Constants.programFilesx86));
            Assert.AreEqual(actualMsBuildPath, msbuildpath);
        }

        [Test]
        public void TestMissingTargets()
        {
            string msbuildpath = msBuildDetector.GetFirstMatchingMsBuildFromPath(programFilesPath: Path.Combine(testMissingTargetsPath, Constants.programFiles), programFilesX86Path: Path.Combine(testMissingTargetsPath, Constants.programFilesx86));
            Assert.IsNull(msbuildpath);
        }

        [Test]
        public void TestGetMSBuildPathOnWindows()
        {
            // test with VS2022 path
            string actualMsBuildPath = Path.Combine(testvs2022Path, Constants.vs2022MSBuildPath);
            string msbuildpath = workspaceBuilderHelper.GetMSBuildPathEnvironmentVariable(OSPlatform.Windows, 
                programFilesPath: Path.Combine(testvs2022Path, Constants.programFiles), programFilesX86Path: Path.Combine(testvs2022Path, Constants.programFilesx86));
            Assert.AreEqual(actualMsBuildPath, msbuildpath);
        }

        [Test]
        public void TestMissingMSBuildOnWindows()
        {
            string errorMessage = "Codelyzer wasn't able to retrieve the MSBuild path. Visual Studio and MSBuild might not be installed.";
            string msbuildpath = workspaceBuilderHelper.GetMSBuildPathEnvironmentVariable(OSPlatform.Windows, 
                programFilesPath: Path.Combine(testMissingTargetsPath, Constants.programFiles), programFilesX86Path: Path.Combine(testMissingTargetsPath, Constants.programFilesx86));
            mockedLogger.Verify(
                mock => mock.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => string.Equals(errorMessage, o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            Assert.IsNull(msbuildpath);
        }

        [Test]
        public void TestGetMSBuildPathOnLinux()
        {
            string msbuildpath = workspaceBuilderHelper.GetMSBuildPathEnvironmentVariable(OSPlatform.Linux);
            Assert.AreEqual(Codelyzer.Analysis.Common.Constants.MsBuildCommandName, msbuildpath);
        }

        [Test]
        public void TestGetMSBuildPathOnOSX()
        {
            string msbuildpath = workspaceBuilderHelper.GetMSBuildPathEnvironmentVariable(OSPlatform.OSX);
            Assert.AreEqual(Codelyzer.Analysis.Common.Constants.MsBuildCommandName, msbuildpath);
        }

        [Test]
        public void TestGetMSBuildPathOnFreeBSD()
        {
            string msbuildpath = workspaceBuilderHelper.GetMSBuildPathEnvironmentVariable(OSPlatform.FreeBSD);
            Assert.AreEqual(Codelyzer.Analysis.Common.Constants.MsBuildCommandName, msbuildpath);
        }
    }

}