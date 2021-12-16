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
using System.Threading;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Assert;
using Codelyzer.Analysis.Build;

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

        [OneTimeSetUp]
        public void Setup()
        {
            Setup(GetType());
            tempDir = GetTstPath(Path.Combine(Constants.TempProjectDirectories));
            SetupTestProject();
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
            string msbuildpath = WorkspaceBuilderHelper.GetFrameworkMsBuildExePath(programFilesPath: Path.Combine(testvs2022Path, Constants.programFiles), programFilesX86Path: Path.Combine(testvs2022Path, Constants.programFilesx86));
            Assert.AreEqual(actualMsBuildPath,msbuildpath);
        }

        [Test]
        public void TestVS2019()
        {
            string actualMsBuildPath = Path.Combine(testvs2019Path, Constants.vs2019MSBuildPath);
            string msbuildpath = WorkspaceBuilderHelper.GetFrameworkMsBuildExePath(programFilesPath: Path.Combine(testvs2019Path, Constants.programFiles), programFilesX86Path: Path.Combine(testvs2019Path, Constants.programFilesx86));
            Assert.AreEqual(actualMsBuildPath, msbuildpath);
        }

        [Test]
        public void TestVS2017And2019BuildTools()
        {
            string actualMsBuildPath = Path.Combine(testvs2017And19BuildToolsPath, Constants.vs2019BuildToolsMSBuildPath);
            string msbuildpath = WorkspaceBuilderHelper.GetFrameworkMsBuildExePath(programFilesPath: Path.Combine(testvs2017And19BuildToolsPath, Constants.programFiles), programFilesX86Path: Path.Combine(testvs2017And19BuildToolsPath, Constants.programFilesx86));
            Assert.AreEqual(actualMsBuildPath, msbuildpath);
        }

        [Test]
        public void TestVS2022MissingTargets()
        {
            string actualMsBuildPath = Path.Combine(testvs2022MissingTargetsPath, Constants.vs2019MSBuildPath);
            string msbuildpath = WorkspaceBuilderHelper.GetFrameworkMsBuildExePath(programFilesPath: Path.Combine(testvs2022MissingTargetsPath, Constants.programFiles), programFilesX86Path: Path.Combine(testvs2022MissingTargetsPath, Constants.programFilesx86));
            Assert.AreEqual(actualMsBuildPath, msbuildpath);
        }

        [Test]
        public void TestMSBuild14()
        {
            string actualMsBuildPath = Path.Combine(testMSBuild14Path, Constants.MSBuild14Path);
            string msbuildpath = WorkspaceBuilderHelper.GetFrameworkMsBuildExePath(programFilesPath: Path.Combine(testMSBuild14Path, Constants.programFiles), programFilesX86Path: Path.Combine(testMSBuild14Path, Constants.programFilesx86));
            Assert.AreEqual(actualMsBuildPath, msbuildpath);
        }

        [Test]
        public void TestMissingTargets()
        {
            string msbuildpath = WorkspaceBuilderHelper.GetFrameworkMsBuildExePath(programFilesPath: Path.Combine(testMissingTargetsPath, Constants.programFiles), programFilesX86Path: Path.Combine(testMissingTargetsPath, Constants.programFilesx86));
            Assert.AreEqual("", msbuildpath);
        }
    }

}