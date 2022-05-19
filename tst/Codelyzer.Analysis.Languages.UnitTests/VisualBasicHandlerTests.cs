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
					MethodInvocations = true,
					InvocationArguments = true,
					DeclarationNodes = true
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

		[Fact]
		public void InvocationExpressionHandlerTest()
		{
			var expressShell = @"
			Class TypeName
				Public Sub Test()
					Dim a = ""test""
					String.Equals(a, ""v"", System.StringComparison.Ordinal)
				End Sub
			End Class";
			var rootNode = GetVisualBasicUstNode(expressShell);
			Assert.Single(rootNode.Children);
			var classBlockNode = rootNode.Children[0];
			Assert.Equal(3, classBlockNode.Children.Count);

			//0:class-statement
			var classStatementNode = classBlockNode.Children[0];
			Assert.True(classStatementNode.GetType() == typeof(Model.ClassStatement));
			//1:sub-block
			var subBlockNode = classBlockNode.Children[1];
			Assert.True(subBlockNode.GetType() == typeof(Model.MethodBlock));
			Assert.Equal("SubBlock", subBlockNode.Identifier);
			Assert.Equal(4, subBlockNode.Children.Count);
			//1:sub-block::0::substatement
			var subStatementNode = subBlockNode.Children[0];
			Assert.True(subStatementNode.GetType() == typeof(Model.MethodStatement));

			//1:sub-block::1::LocalDeclarationStatement
			var localDeclareNode = subBlockNode.Children[1]; 
			Assert.True(localDeclareNode.GetType() == typeof(Model.LocalDeclarationStatement));
			Assert.Single(localDeclareNode.Children);
			Assert.True(localDeclareNode.Children[0].GetType() == typeof(Model.VariableDeclarator));
			Assert.Equal("a = \"test\"", localDeclareNode.Children[0].Identifier);
			//1:sub-block::2::invocation
			var invocationNode = subBlockNode.Children[2];
			Assert.True(invocationNode.GetType() == typeof(Model.InvocationExpression));
			var invocationArgsNode =((Model.InvocationExpression)invocationNode).Arguments;
			Assert.Equal(3, invocationArgsNode.Count);
			Assert.Equal("a", invocationArgsNode[0].Identifier);
			Assert.Equal("\"v\"", invocationArgsNode[1].Identifier);
			Assert.Equal("System.StringComparison.Ordinal", invocationArgsNode[2].Identifier);
			//1:sub-block::3::endSub
			var endSubNode = subBlockNode.Children[3];
			Assert.True(endSubNode.GetType() == typeof(Model.EndBlockStatement));
			Assert.Equal("End Sub", endSubNode.Identifier);
			//2:end-class
			var endBlockNode = classBlockNode.Children[2];
			Assert.True(endBlockNode.GetType() == typeof(Model.EndBlockStatement));
			Assert.Equal("End Class", endBlockNode.Identifier);
		}


		[Fact]
		public void AttributeHandlerTest()
		{
			var expressShell = @"
				<AttributeUsage(AttributeTargets.[Class], AllowMultiple:=True)>
				Public Class AuthorAttribute
					Inherits Attribute

					Private name As String

					Public Sub New(ByVal name As String)
						Me.name = name
					End Sub

					Public ReadOnly Property Name As String
						Get
							Return name
						End Get
					End Property
				End Class";
			var rootNode = GetVisualBasicUstNode(expressShell);
			Assert.Single(rootNode.Children);
			/*var classNode = rootNode.Children[0];
			var annotationNode = classNode.Children[0];
			Assert.Equal(typeof(Model.Annotation), annotationNode.GetType());
			Assert.Equal(2, annotationNode.Children.Count);
			Assert.Equal("AttributeTargets.[Class]", annotationNode.Children[0].Identifier);
			Assert.Equal("AllowMultiple:=True", annotationNode.Children[1].Identifier);

			var constructionNode = rootNode.Children[1];
			Assert.Equal(typeof(Model.ConstructorDeclaration), constructionNode.GetType());
			Assert.Equal("AuthorAttribute", constructionNode.Identifier);

			var returnNode = rootNode.Children[2];
			Assert.Equal(typeof(Model.ReturnStatement), returnNode.GetType());
			Assert.Equal("name", returnNode.Identifier);*/
		}


		[Fact]
		public void FunctionHandlerTest()
		{
			var expressShell = @"
			Private Function GenerateResponse(context As HttpContext)
				Return ""This is a test handler""
			End Function";
			var rootNode = GetVisualBasicUstNode(expressShell);
			Assert.Single(rootNode.Children);
			var funcBlockNode = rootNode.Children[0];
			Assert.True(funcBlockNode.GetType() == typeof(Model.MethodBlock));
			Assert.Equal("FunctionBlock", funcBlockNode.Identifier);
			Assert.Equal(3, funcBlockNode.Children.Count);
			//1:sub-block::0::substatement
			var subStatementNode = funcBlockNode.Children[0];
			Assert.True(subStatementNode.GetType() == typeof(Model.MethodStatement));

			var returnNode = funcBlockNode.Children[1];
			Assert.True(returnNode.GetType() == typeof(Model.ReturnStatement));
			Assert.Single(returnNode.Children);
			var literalNode = returnNode.Children[0];
			Assert.True(literalNode.GetType() == typeof(Model.LiteralExpression));
			var endBlockNode = funcBlockNode.Children[2];
			Assert.True(endBlockNode.GetType() == typeof(Model.EndBlockStatement));
			Assert.Equal("End Function", endBlockNode.Identifier);
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
