using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Codelyzer.Analysis.Build;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Moq;

namespace Codelyzer.Analysis.Tests
{
    public class ProjectBuildHandlerTests
    {
        protected string projectFileContent;
        protected string tmpTestFixturePath;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            projectFileContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include=""Microsoft.AspNetCore.App"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""MyFirstPackage"" Version=""1.0.0"" />
    <PackageReference Include=""MySecondPackage"" Version=""2.0.0"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""TheMainProject"" />
    <ProjectReference Include=""TheDependency"" />
  </ItemGroup>
  <ItemGroup Label=""PortingInfo"">
    <!-- DO NOT REMOVE WHILE PORTING
        C:\\RandomFile.dll
        C:\\this\\is\\some\\path\\to\\Some.dll
    -->
  </ItemGroup>
</Project>";
            tmpTestFixturePath = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()));
            Directory.CreateDirectory(tmpTestFixturePath);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(tmpTestFixturePath, true);
        }


        [Test]
        public void ExtractFileReferencesFromProject_Retrieves_Expected_ReferencePaths()
        {
            using var stringReader = new StringReader(projectFileContent);
            var projectFileDoc = XDocument.Load(stringReader);
            var projectBuildHandlerInstance = new ProjectBuildHandler(null);
            var extractFileReferencesFromProjectMethod =
                TestUtils.GetPrivateMethod(projectBuildHandlerInstance.GetType(), "ExtractFileReferencesFromProject");

            // Invoke method and read contents of method output
            var fileReferences = (List<string>)extractFileReferencesFromProjectMethod.Invoke(projectBuildHandlerInstance, new object[] { projectFileDoc });
            var expectedFileReferences = new List<string>
            {
                @"C:\\RandomFile.dll",
                @"C:\\this\\is\\some\\path\\to\\Some.dll"
            };

            CollectionAssert.AreEquivalent(expectedFileReferences, fileReferences);
        }

        [Test]
        public void LoadProjectFile_Returns_Expected_XDocument()
        {
            var testProjectFilePath = Path.GetFullPath(Path.Combine(
                tmpTestFixturePath,
                "ProjectFileWithNonExistingMetaReferences.xml"
                ));
            File.WriteAllText(testProjectFilePath, projectFileContent);
            var projectBuildHandlerInstance = new ProjectBuildHandler(null);
            var loadProjectFileMethod =
                TestUtils.GetPrivateMethod(projectBuildHandlerInstance.GetType(), "LoadProjectFile");

            // Invoke method and read contents of method output
            var projectFile = (XDocument)loadProjectFileMethod.Invoke(projectBuildHandlerInstance, new object[] { testProjectFilePath });

            Assert.AreEqual(projectFileContent, projectFile.ToString());
        }


        [Test]
        public void LoadProjectFile_Returns_Null_On_Invalid_ProjectFilePath()
        {
            var projectBuildHandlerInstance = new ProjectBuildHandler(null);
            var loadProjectFileMethod =
                TestUtils.GetPrivateMethod(projectBuildHandlerInstance.GetType(), "LoadProjectFile");

            // Invoke method and read contents of method output
            var projectFile = (XDocument)loadProjectFileMethod.Invoke(projectBuildHandlerInstance, new object[] { @"C:\\Invalid\\ProjectFilePath.csproj" });
            
            Assert.AreEqual(null, projectFile);
        }
        [Test]
        public void LoadProjectFile_Returns_Null_On_Invalid_ProjectFileContent()
        {
            var testProjectFilePath = Path.GetFullPath(Path.Combine(
                tmpTestFixturePath,
                "InvalidProjectFile.xml"
                ));
            File.WriteAllText(testProjectFilePath, "Invalid Project File Content!!!");

            var mockedLogger = new Mock<ILogger>();
            var projectBuildHandlerInstance = new ProjectBuildHandler(mockedLogger.Object, null, new List<string>());
            var loadProjectFileMethod =
                TestUtils.GetPrivateMethod(projectBuildHandlerInstance.GetType(), "LoadProjectFile");

            // Invoke method and read contents of method output
            var projectFile = (XDocument)loadProjectFileMethod.Invoke(projectBuildHandlerInstance, new object[] { testProjectFilePath });

            Assert.AreEqual(null, projectFile);
        }

        [Test]
        public void LoadMetadataReferences_Returns_Empty_On_Invalid_ProjectFile()
        {
            var projectBuildHandlerInstance = new ProjectBuildHandler(null);
            var loadMetadataReferencesMethod =
                TestUtils.GetPrivateMethod(projectBuildHandlerInstance.GetType(), "LoadMetadataReferences");

            // Invoke method and read contents of method output
            var metadataReferences = (List<PortableExecutableReference>)loadMetadataReferencesMethod.Invoke(projectBuildHandlerInstance, new object[] { null });
            var expectedMetadataReferences = new List<PortableExecutableReference>();

            CollectionAssert.AreEquivalent(expectedMetadataReferences, metadataReferences);
        }

        [Test]
        public void LoadMetadataReferences_Returns_Empty_On_Invalid_ReferencePath()
        {
            var projectFileDoc = XDocument.Load(new StringReader(projectFileContent));

            var mockedLogger = new Mock<ILogger>();
            var projectBuildHandlerInstance = new ProjectBuildHandler(mockedLogger.Object, null, new List<string>());
            var loadMetadataReferencesMethod =
                TestUtils.GetPrivateMethod(projectBuildHandlerInstance.GetType(), "LoadMetadataReferences");

            // Invoke method and read contents of method output
            var metadataReferences = (List<PortableExecutableReference>)loadMetadataReferencesMethod.Invoke(projectBuildHandlerInstance, new object[] { projectFileDoc });
            var expectedMetadataReferences = new List<PortableExecutableReference>();

            CollectionAssert.AreEquivalent(expectedMetadataReferences, metadataReferences);
        }

        [Test]
        public void LoadMetadataReferences_Returns_Expected_ReferencePath()
        {
            var testReferencePath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "Codelyzer.Analysis.Tests.dll"
                );
            var projectFileContent = string.Format(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include=""Microsoft.AspNetCore.App"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""MyFirstPackage"" Version=""1.0.0"" />
    <PackageReference Include=""MySecondPackage"" Version=""2.0.0"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""TheMainProject"" />
    <ProjectReference Include=""TheDependency"" />
  </ItemGroup>
  <ItemGroup Label=""PortingInfo"">
    <!-- DO NOT REMOVE WHILE PORTING
        C:\\RandomFile.dll
        {0}
    -->
  </ItemGroup>
</Project>", testReferencePath);
            var projectFileDoc = XDocument.Load(new StringReader(projectFileContent));

            var mockedLogger = new Mock<ILogger>();
            var projectBuildHandlerInstance = new ProjectBuildHandler(mockedLogger.Object, null, new List<string>());
            var loadMetadataReferencesMethod =
                TestUtils.GetPrivateMethod(projectBuildHandlerInstance.GetType(), "LoadMetadataReferences");

            // Invoke method and read contents of method output
            var metadataReferences = (List<PortableExecutableReference>)loadMetadataReferencesMethod.Invoke(projectBuildHandlerInstance, new object[] { projectFileDoc });
            Assert.AreEqual(1, metadataReferences.Count);
        }
    }
}
