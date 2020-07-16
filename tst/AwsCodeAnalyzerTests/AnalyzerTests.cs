using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog.Core;

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
    }
}