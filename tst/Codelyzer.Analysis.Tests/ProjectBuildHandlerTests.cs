using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Codelyzer.Analysis.Build;
using Codelyzer.Analysis.Common;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
            projectFileContent = @"<Project Sdk=""Microsoft.NET.Sdk"">" + Environment.NewLine + 
@"  <PropertyGroup>" + Environment.NewLine + 
@"    <TargetFramework>netcoreapp3.1</TargetFramework>" + Environment.NewLine + 
@"  </PropertyGroup>" + Environment.NewLine + 
@"  <ItemGroup>" + Environment.NewLine + 
@"    <FrameworkReference Include=""Microsoft.AspNetCore.App"" />" + Environment.NewLine + 
@"  </ItemGroup>" + Environment.NewLine + 
@"  <ItemGroup>" + Environment.NewLine + 
@"    <PackageReference Include=""MyFirstPackage"" Version=""1.0.0"" />" + Environment.NewLine + 
@"    <PackageReference Include=""MySecondPackage"" Version=""2.0.0"" />" + Environment.NewLine + 
@"  </ItemGroup>" + Environment.NewLine + 
@"  <ItemGroup>" + Environment.NewLine + 
@"    <ProjectReference Include=""TheMainProject"" />" + Environment.NewLine + 
@"    <ProjectReference Include=""TheDependency"" />" + Environment.NewLine + 
@"  </ItemGroup>" + Environment.NewLine + 
@"  <ItemGroup Label=""PortingInfo"">" + Environment.NewLine + 
@"    <!-- DO NOT REMOVE WHILE PORTING" + Environment.NewLine + 
@"        C:\\RandomFile.dll" + Environment.NewLine + 
@"        C:\\this\\is\\some\\path\\to\\Some.dll" + Environment.NewLine + 
@"    -->" + Environment.NewLine +
@"  </ItemGroup>" + Environment.NewLine +
@"</Project>";
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
            var projectBuildHelperInstance = new ProjectBuildHelper(NullLogger.Instance);
            var extractFileReferencesFromProjectMethod =
                TestUtils.GetPrivateMethod(projectBuildHelperInstance.GetType(), "ExtractFileReferencesFromProject");

            // Invoke method and read contents of method output
            var fileReferences = (List<string>)extractFileReferencesFromProjectMethod.Invoke(projectBuildHelperInstance, new object[] { projectFileDoc });
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
            var projectBuildHelperInstance = new ProjectBuildHelper(null);
            var loadProjectFileMethod =
                TestUtils.GetPrivateMethod(projectBuildHelperInstance.GetType(), "LoadProjectFile");

            // Invoke method and read contents of method output
            var projectFile = (XDocument)loadProjectFileMethod.Invoke(projectBuildHelperInstance, new object[] { testProjectFilePath });

            Assert.AreEqual(projectFileContent, projectFile.ToString());
        }


        [Test]
        public void LoadProjectFile_Returns_Null_On_Invalid_ProjectFilePath()
        {
            var projectBuildHelperInstance = new ProjectBuildHelper(null);
            var loadProjectFileMethod =
                TestUtils.GetPrivateMethod(projectBuildHelperInstance.GetType(), "LoadProjectFile");

            // Invoke method and read contents of method output
            var projectFile = (XDocument)loadProjectFileMethod.Invoke(projectBuildHelperInstance, new object[] { @"C:\\Invalid\\ProjectFilePath.csproj" });
            
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
            var projectBuildHelperInstance = new ProjectBuildHelper(mockedLogger.Object);
            var loadProjectFileMethod =
                TestUtils.GetPrivateMethod(projectBuildHelperInstance.GetType(), "LoadProjectFile");

            // Invoke method and read contents of method output
            var projectFile = (XDocument)loadProjectFileMethod.Invoke(projectBuildHelperInstance, new object[] { testProjectFilePath });

            Assert.AreEqual(null, projectFile);
        }

        [Test]
        public void LoadMetadataReferences_Returns_Empty_On_Invalid_ProjectFile()
        {
            var projectBuildHelperInstance = new ProjectBuildHelper(null);
            var (metadataReferences, _) = projectBuildHelperInstance.LoadMetadataReferences(null);
            var expectedMetadataReferences = new List<PortableExecutableReference>();
            CollectionAssert.AreEquivalent(expectedMetadataReferences, (List<PortableExecutableReference>)metadataReferences);
        }

        [Test]
        public void LoadMetadataReferences_Returns_Empty_On_Invalid_ReferencePath()
        {
            var projectFileDoc = XDocument.Load(new StringReader(projectFileContent));

            var mockedLogger = new Mock<ILogger>();
            var projectBuildHelperInstance = new ProjectBuildHelper(mockedLogger.Object);
            
            // Invoke method and read contents of method output
            var (metadataReferences, missingMetaReferences) = projectBuildHelperInstance.LoadMetadataReferences(projectFileDoc);
            var expectedMetadataReferences = new List<PortableExecutableReference>();
            CollectionAssert.AreEquivalent(expectedMetadataReferences, metadataReferences);

            // Validate MissingMetaReferences
            List<string> expectedMissingMetaReferences = new List<string> { @"C:\\RandomFile.dll", @"C:\\this\\is\\some\\path\\to\\Some.dll" };
            CollectionAssert.AreEquivalent(expectedMissingMetaReferences, missingMetaReferences);
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
            var projectBuildHelperInstance = new ProjectBuildHelper(mockedLogger.Object);

            // Invoke method and read contents of method output
            var (metadataReferences, missingMetaReferences) =
                projectBuildHelperInstance.LoadMetadataReferences(projectFileDoc);
            Assert.AreEqual(1, metadataReferences.Count);

            // Validate MissingMetaReferences
            List<string> expectedMissingMetaReferences = new List<string> { @"C:\\RandomFile.dll" };
            CollectionAssert.AreEquivalent(expectedMissingMetaReferences, missingMetaReferences);
        }
    }
}
