using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog.Core;
using System.Linq;
using System.Net.Http;
using System.IO;
using System.IO.Compression;

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
            DownloadSampleWebApi();
        }

        private void DownloadSampleWebApi()
        {
            var link = @"https://github.com/FabianGosebrink/ASPNET-WebAPI-Sample/archive/master.zip";
            using (var client = new HttpClient())
            {
                var content = client.GetByteArrayAsync(link).Result;
                var tempDirectory = Directory.CreateDirectory(GetPath(@"Projects\Temp"));
                var fileName = string.Concat(tempDirectory.FullName, @"\ASPNET-WebAPI-Sample-master.zip");
                File.WriteAllBytes(fileName, content);
                ZipFile.ExtractToDirectory(fileName, tempDirectory.FullName, true);
                File.Delete(fileName);
            }
        }

        [Test]
        public async Task TestAnalyzer()
        {
            string projectPath = GetPath(@"Projects\CodelyzerDummy\CodelyzerDummy.csproj");

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
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, Logger.None);
            AnalyzerResult result = await analyzer.AnalyzeProject(projectPath);
            Assert.True(result != null);
        }



        [Test]
        public async Task TestSampleWebApi()
        {
            string projectPath = GetPath(@"Projects\Temp\ASPNET-WebAPI-Sample-master\SampleWebApi\SampleWebApi.csproj");
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
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, Logger.None);
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

            Directory.Delete(GetPath(@"Projects\Temp\ASPNET-WebAPI-Sample-master"), true);
        }
    }
}