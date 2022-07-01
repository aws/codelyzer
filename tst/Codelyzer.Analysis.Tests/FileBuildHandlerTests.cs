using System.Collections.Generic;
using System.Linq;
using Codelyzer.Analysis.Build;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Codelyzer.Analysis.Tests
{
    public class FileBuildHandlerTests
    {
        [Test]
        public void TestCsharpFileBuildHandler()
        {
            var projectPath = "test.csproj";
            var testFile = $@"
using System;
namespace Test
{{
    public class TestClass {{}}
}}";
            var fileInfo = new Dictionary<string, string>
            {
                {"TestFile.cs", testFile}
            };
            var fileBuildHandler = new FileBuildHandler(NullLogger.Instance, projectPath, fileInfo, new List<string>(), new List<string>());
            var result= fileBuildHandler.Build().Result;
            Assert.IsTrue(result.Count == 1);
            Assert.IsTrue(result.First().SyntaxTree is CSharpSyntaxTree);
        }

        [Test]
        public void TestVisualBasicFileBuildHandler()
        {
            var projectPath = "test.vbproj";
            var testFile = $@"
Class TestClass
    Public Sub Main() 
        Console.WriteLine(1)
    End Sub
End Class";
            var fileInfo = new Dictionary<string, string>
            {
                {"TestFile.vb", testFile}
            };
            var fileBuildHandler = new FileBuildHandler(NullLogger.Instance, projectPath, fileInfo, new List<string>(), new List<string>());
            var result= fileBuildHandler.Build().Result;
            Assert.IsTrue(result.Count == 1);
            Assert.IsTrue(result.First().SyntaxTree is VisualBasicSyntaxTree);
        }
    
    }
}