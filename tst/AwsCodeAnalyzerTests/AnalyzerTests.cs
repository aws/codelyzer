using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog.Core;
using System.Linq;

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
        public async Task TestMvcMusicStore()
        {
            string projectPath = GetPath(@"Projects\MvcMusicStore\MvcMusicStore\MvcMusicStore.csproj");

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

            var accountController = result.ProjectResult.SourceFileResults.Where(f => f.FilePath.EndsWith("AccountController.cs")).FirstOrDefault();
            Assert.NotNull(accountController);

            var classDeclaration = accountController.Children.OfType<AwsCodeAnalyzer.Model.NamespaceDeclaration>().FirstOrDefault().Children[0];
            Assert.NotNull(classDeclaration);

            var declarationNodes = classDeclaration.Children.OfType<AwsCodeAnalyzer.Model.DeclarationNode>();
            var attributeNodes = classDeclaration.Children.OfType<AwsCodeAnalyzer.Model.Annotation>();

            Assert.AreEqual(declarationNodes.Count(), 15);
            Assert.AreEqual(attributeNodes.Count(), 5);
        }
    }
}