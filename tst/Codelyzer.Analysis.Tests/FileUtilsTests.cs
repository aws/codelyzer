using System.IO;
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
    }
}
