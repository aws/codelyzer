using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Assert;

namespace AwsCodeAnalyzer.Tests
{
    //Implementations

    [TestFixture()]
    [NonParallelizable]
    public class AwsAnalyzerTests : AwsBaseTest
    {
        [SetUp]
        public void BaseSetUp()
        {
            Setup(this.GetType());
            DownloadTempProjects();
        }

        private void DownloadTempProjects()
        {
            DownloadFromGitHub(@"https://github.com/FabianGosebrink/ASPNET-WebAPI-Sample/archive/master.zip", "ASPNET-WebAPI-Sample-master");
            DownloadFromGitHub(@"https://github.com/carlosfigueira/WCFSamples/archive/master.zip", "WCFSamples-master");
        }       

        private void DownloadFromGitHub(string link, string name)
        {
            using (var client = new HttpClient())
            {
                var content = client.GetByteArrayAsync(link).Result;
                var tempDirectory = Directory.CreateDirectory(GetPath(new string[] { "Projects", "Temp" }));
                var fileName = string.Concat(tempDirectory.FullName, name, @".zip");
                File.WriteAllBytes(fileName, content);
                ZipFile.ExtractToDirectory(fileName, tempDirectory.FullName, true);
                File.Delete(fileName);
            }
        }

        [Test]
        public async Task TestAnalyzer()
        {
            string projectPath = string.Concat(GetPath(new string[] { "Projects", "CodelyzerDummy", "CodelyzerDummy" }),".csproj");

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                ExportSettings =
                {
                    GenerateJsonOutput = true,
                    OutputPath = @"/tmp/UnitTests"
                },

                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true
                }
            };
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, Serilog.Core.Logger.None);
            AnalyzerResult result = await analyzer.AnalyzeProject(projectPath);
            Assert.True(result != null);
        }



        [Test]
        public async Task TestWCFProjects()
        {
            string mainDir = GetPath(new string[] { "Projects", "Temp", "WCFSamples-master" });
            DirectoryAssert.Exists(mainDir);

            //Find all solutions
            List<string> solutionsPath = Directory.EnumerateFiles(mainDir, "*.sln", SearchOption.AllDirectories).ToList();


            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                ExportSettings =
                {
                    GenerateJsonOutput = true,
                    OutputPath = @"/tmp/UnitTests"
                },

                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true,
                    Annotations = true,
                    DeclarationNodes = true,
                    LocationData = false
                }
            };
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, Serilog.Core.Logger.None);

            foreach (var solution in solutionsPath)
            {
                var result = await analyzer.AnalyzeSolution(solution);
                Assert.NotNull(result);

                TestContext.WriteLine(string.Format("Building {0}", solution));

                foreach (var r in result)
                {
                    var projectResult = r.ProjectResult;
                    Assert.NotNull(projectResult);

                    if(projectResult.BuildErrors.Count > 0)
                    {
                        TestContext.WriteLine(string.Format("Error building {0}", projectResult.ProjectFilePath));
                    }
                    Assert.AreEqual(projectResult.BuildErrors.Count, 0);                    
                }

            }

            Directory.Delete((mainDir), true);
        }

        [Test]
        public async Task TestSampleWebApi()
        {
            string projectPath = string.Concat(GetPath(new string[] { "Projects", "Temp", "ASPNET-WebAPI-Sample-master", "SampleWebApi", "SampleWebApi" })
                , ".csproj");
            FileAssert.Exists(projectPath);

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                ExportSettings =
                {
                    GenerateJsonOutput = true,
                    OutputPath = @"/tmp/UnitTests"
                },

                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true,
                    Annotations = true,
                    DeclarationNodes = true,
                    LocationData = false
                }
            };
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, Serilog.Core.Logger.None);
            AnalyzerResult result = await analyzer.AnalyzeProject(projectPath);
            Assert.True(result != null);

            var houseController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("HouseController.cs")).FirstOrDefault();
            Assert.NotNull(houseController);

            var classDeclarations = houseController.Children.OfType<AwsCodeAnalyzer.Model.NamespaceDeclaration>().FirstOrDefault();
            Assert.Greater(classDeclarations.Children.Count, 0);

            var classDeclaration = houseController.Children.OfType<AwsCodeAnalyzer.Model.NamespaceDeclaration>().FirstOrDefault().Children[0];
            Assert.NotNull(classDeclaration);

            var declarationNodes = classDeclaration.Children.OfType<AwsCodeAnalyzer.Model.DeclarationNode>();
            var attributeNodes = classDeclaration.Children.OfType<AwsCodeAnalyzer.Model.Annotation>();

            //HouseController has 20 identifiers declared within the class declaration:
            Assert.AreEqual(declarationNodes.Count(), 20);

            //HouseController has 17 attributes:
            Assert.AreEqual(attributeNodes.Count(), 17);

            Directory.Delete(GetPath(new string[] { "Projects", "Temp", "ASPNET-WebAPI-Sample-master" }), true);
        }
    }
}