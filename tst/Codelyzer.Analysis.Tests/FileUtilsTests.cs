using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using NUnit.Framework;

namespace Codelyzer.Analysis.Tests
{
    [TestFixture]
    public class FileUtilsTests
    {
        private const string SourceDirPath = "FileUtilsTests_SourceDir";
        private const string DestDirPath = "FileUtilsTests_DestDir";

        [SetUp]
        public void SetUp()
        {
            RemoveTestDirs();
        }

        [TearDown]
        public void TearDown()
        {
            RemoveTestDirs();
        }

        private static void RemoveTestDirs()
        {
            if (Directory.Exists(DestDirPath))
            {
                Directory.Delete(DestDirPath, true);
            }
            if (Directory.Exists(SourceDirPath))
            {
                Directory.Delete(SourceDirPath, true);
            }
        }

        [Test]
        public void DirectoryCopy_CreatesNewDirectory_IfDestDoesNotExists()
        {
            // Arrange
            Directory.CreateDirectory(SourceDirPath);
            if (Directory.Exists(DestDirPath))
            {
                Directory.Delete(DestDirPath);
            }

            // Act
            FileUtils.DirectoryCopy(SourceDirPath, DestDirPath, false);

            // Assert
            Directory.Exists(DestDirPath);
        }

        [Test]
        public void DirectoryCopy_Throws_IfSourceDoesNotExists()
        {
            // Arrange
            if (Directory.Exists(SourceDirPath))
            {
                Directory.Delete(SourceDirPath);
            }

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => FileUtils.DirectoryCopy(SourceDirPath, DestDirPath, false));
        }

        [Test]
        public void DirectoryCopy_CopiesContainedFiles()
        {
            // Arrange
            const string testFileName = "testFile.txt";
            Directory.CreateDirectory(SourceDirPath);
            var filePath = Path.Combine(SourceDirPath, testFileName);
            File.Create(filePath).Dispose();

            // Act
            FileUtils.DirectoryCopy(SourceDirPath, DestDirPath, true);

            // Assert
            Assert.IsTrue(File.Exists(Path.Combine(DestDirPath, testFileName)));
        }

        [Test]
        public void DirectoryCopy_CopiesSubFolders()
        {
            // Arrange
            const string subFolderName = "SubFolder";
            const string testFileName = "testFile.txt";
            Directory.CreateDirectory(SourceDirPath);
            var subDir = Directory.CreateDirectory(Path.Combine(SourceDirPath, subFolderName)).FullName;
            var filePath = Path.Combine(subDir, testFileName);
            File.Create(filePath).Dispose();

            // Act
            FileUtils.DirectoryCopy(SourceDirPath, DestDirPath, true);

            // Assert
            Assert.IsTrue(File.Exists(Path.Combine(DestDirPath, subFolderName, testFileName)));
        }

        [Test]
        public void GetProjectPathsFromSolutionFile_ParsesSLN()
        {
            // Arrange
            const string testFileName = "test.sln";
            Directory.CreateDirectory(SourceDirPath);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), SourceDirPath, testFileName);

            using (FileStream fs = File.Create(filePath))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(Constants.SampleSolutionFile);
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }

            // Act
            var results = FileUtils.GetProjectPathsFromSolutionFile(filePath);

            // Assert
            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count() == 5);
        }

        [Test]
        public void GetProjectPathsFromSolutionFile_FailsParseSLN()
        {
            // Arrange
            const string testFileName = "testFile.sln";
            const string projectFileName = "testProject.csproj";
            Directory.CreateDirectory(SourceDirPath);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), SourceDirPath, testFileName);
            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), SourceDirPath, projectFileName);
            File.Create(filePath).Dispose();
            File.Create(projectPath).Dispose();

            // Act
            var results = FileUtils.GetProjectPathsFromSolutionFile(filePath);

            // Assert
            Assert.IsNotNull(results);
            Assert.IsTrue(results.FirstOrDefault() == projectPath);
            Assert.IsTrue(File.Exists(projectPath));
        }

        [Test]
        public void GetProjectPathsFromSolutionFile_NoSLNFile()
        {
            // Arrange
            const string testFileName = "testFile.txt";
            const string projectFileName = "testProject.csproj";
            Directory.CreateDirectory(SourceDirPath);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), SourceDirPath, testFileName);
            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), SourceDirPath, projectFileName);
            File.Create(filePath).Dispose();
            File.Create(projectPath).Dispose();

            // Act
            var results = FileUtils.GetProjectPathsFromSolutionFile(filePath);

            // Assert
            Assert.IsNotNull(results);
            Assert.IsTrue(results.FirstOrDefault() == projectPath);
            Assert.IsTrue(File.Exists(projectPath));
        }

        [Test]
        public void TestGetRelativePath()
        {
            // FileUtils.GetRelativePath is needed because .NET Standard 2.0 does not have Path.GetRelativePath.
            // Testing against actual method for correctness check.

            const string testFileName = "testRelativePathFile.txt";
            Directory.CreateDirectory(SourceDirPath);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), SourceDirPath, testFileName);
            File.Create(filePath).Dispose();

            var actual = PathNetCore.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
            var expected = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
            Assert.AreEqual(expected, actual);

            var actual2 = PathNetCore.GetRelativePath("Q:\\", filePath);
            var expected2 = Path.GetRelativePath("Q:\\", filePath);

            Assert.AreEqual(expected2, actual2);
        }

        [Test]
        [TestCase(@"C:\", @"C:\", @".")]
        [TestCase(@"C:\a", @"C:\a\", @".")]
        [TestCase(@"C:\A", @"C:\a\", @".")]
        [TestCase(@"C:\a\", @"C:\a", @".")]
        [TestCase(@"C:\", @"C:\b", @"b")]
        [TestCase(@"C:\a", @"C:\b", @"..\b")]
        [TestCase(@"C:\a", @"C:\b\", @"..\b\")]
        [TestCase(@"C:\a\b", @"C:\a", @"..")]
        [TestCase(@"C:\a\b", @"C:\a\", @"..")]
        [TestCase(@"C:\a\b\", @"C:\a", @"..")]
        [TestCase(@"C:\a\b\", @"C:\a\", @"..")]
        [TestCase(@"C:\a\b\c", @"C:\a\b", @"..")]
        [TestCase(@"C:\a\b\c", @"C:\a\b\", @"..")]
        [TestCase(@"C:\a\b\c", @"C:\a", @"..\..")]
        [TestCase(@"C:\a\b\c", @"C:\a\", @"..\..")]
        [TestCase(@"C:\a\b\c\", @"C:\a\b", @"..")]
        [TestCase(@"C:\a\b\c\", @"C:\a\b\", @"..")]
        [TestCase(@"C:\a\b\c\", @"C:\a", @"..\..")]
        [TestCase(@"C:\a\b\c\", @"C:\a\", @"..\..")]
        [TestCase(@"C:\a\", @"C:\b", @"..\b")]
        [TestCase(@"C:\a", @"C:\a\b", @"b")]
        [TestCase(@"C:\a", @"C:\A\b", @"b")]
        [TestCase(@"C:\a", @"C:\b\c", @"..\b\c")]
        [TestCase(@"C:\a\", @"C:\a\b", @"b")]
        [TestCase(@"C:\", @"D:\", @"D:\")]
        [TestCase(@"C:\", @"D:\b", @"D:\b")]
        [TestCase(@"C:\", @"D:\b\", @"D:\b\")]
        [TestCase(@"C:\a", @"D:\b", @"D:\b")]
        [TestCase(@"C:\a\", @"D:\b", @"D:\b")]
        [TestCase(@"C:\ab", @"C:\a", @"..\a")]
        [TestCase(@"C:\a", @"C:\ab", @"..\ab")]
        [TestCase(@"C:\", @"\\LOCALHOST\Share\b", @"\\LOCALHOST\Share\b")]
        [TestCase(@"\\LOCALHOST\Share\a", @"\\LOCALHOST\Share\b", @"..\b")]
        //[PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific paths
        public static void GetRelativePath_Windows(string relativeTo, string path, string expected)
        {
            string result = PathNetCore.GetRelativePath(relativeTo, path);
            Assert.AreEqual(Path.GetRelativePath(relativeTo, path), result);

            // Check that we get the equivalent path when the result is combined with the sources
            Assert.IsTrue(
                Path.GetFullPath(path)
                    .TrimEnd(Path.DirectorySeparatorChar)
                    .Equals(
                        Path.GetFullPath(Path.Combine(Path.GetFullPath(relativeTo),
                                result))
                            .TrimEnd(Path.DirectorySeparatorChar),
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}
