using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Codelyzer.Analysis.Common;
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
    }
}
