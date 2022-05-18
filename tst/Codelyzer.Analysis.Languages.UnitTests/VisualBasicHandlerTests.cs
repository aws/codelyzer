using Xunit;
using Microsoft.CodeAnalysis;
using Codelyzer.Analysis.Common;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Codelyzer.Analysis.VisualBasic;

namespace Codelyzer.Analysis.Languages.UnitTests
{
	public class VisualBasicHandlerTests
	{
		private AnalyzerConfiguration Configuration;
		private readonly ITestOutputHelper _output;

		public VisualBasicHandlerTests(ITestOutputHelper output)
		{
			_output = output;
			Configuration = new AnalyzerConfiguration(LanguageOptions.Vb)
			{
				ExportSettings =
				{
					GenerateJsonOutput = false,
				},

				MetaDataSettings =
				{
					LiteralExpressions = true,
					MethodInvocations = true
				}
			};
		}

		[Fact]
		public void ClassStatementHandlerTest()
        {
            const string vbCodeSnippet = @"
				Class Book
				End Class";
            var rootNode = GetVisualBasicUstNode(vbCodeSnippet);
			Assert.Single(rootNode.Children);
			var classNode = rootNode.Children[0];
			Assert.Equal("class-block", classNode.NodeType);
			Assert.Equal("class-statement", classNode.Children[0].NodeType);
		}

		[Fact]
		public void ImportsHandlerTest()
		{
			const string vbCodeSnippet = @"
				Imports System.ComponentModel.Design";
			var rootNode = GetVisualBasicUstNode(vbCodeSnippet);
			Assert.Single(rootNode.Children);
			var importNode = rootNode.Children[0];
			Assert.Equal("imports-statement", importNode.NodeType);
			Assert.True(importNode.GetType() == typeof(Model.ImportsStatement));
		}

		[Fact]
		public void FieldDeclarationHandlerTest()
		{
			const string vbCodeSnippet = 
				@"Public FirstName, LastName As String, Age As Integer";
			var rootNode = GetVisualBasicUstNode(vbCodeSnippet);
			Assert.Single(rootNode.Children);
			var declarationNode = rootNode.Children[0];
			Assert.Equal("field-declaration", declarationNode.NodeType);
			Assert.True(declarationNode.GetType() == typeof(Model.FieldDeclaration));
			Assert.True(declarationNode.Children.Count >0, "declaration should contain variable node");
			var variableNode = declarationNode.Children[0];

			Assert.Equal("variable-declaration", variableNode.NodeType);
			Assert.True(variableNode.GetType() == typeof(Model.VariableDeclarator));
		}

		private Model.UstNode GetVisualBasicUstNode(string expressionShell)
        {
            var tree = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseSyntaxTree(expressionShell);
            var compilation = VisualBasicCompilation.Create(
                "test.dll",
                options: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { tree },
                references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            // Check for errors
            //var diagnostics = compilation.GetDiagnostics();

            CodeContext codeContext = new CodeContext(null, null, tree, null, null, Configuration, new NullLogger<VisualBasicRoslynProcessor>());
            VisualBasicRoslynProcessor processor = new VisualBasicRoslynProcessor(codeContext);

            var result = processor.Visit(codeContext.SyntaxTree.GetRoot());
            return result;
        }
    }
}
