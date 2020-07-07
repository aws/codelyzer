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

            AnalyzerOptions options = new AnalyzerOptions(AnalyzerOptions.LANGUAGE_CSHARP);
            options.JsonOutputPath = "/tmp/unittests";
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(options, Logger.None);
            AnalyzerResult result = await analyzer.AnalyzeProject(projectPath);
            Assert.True(result != null);
        }
    }
}