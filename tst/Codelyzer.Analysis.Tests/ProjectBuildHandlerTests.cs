using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Codelyzer.Analysis.Build;
using NUnit.Framework;

namespace Codelyzer.Analysis.Tests
{
    public class ProjectBuildHandlerTests
    {
        [Test]
        public void ExtractFileReferencesFromProject_Retrieves_Expected_ReferencePaths()
        {
            var projectFileContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
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
    }
}