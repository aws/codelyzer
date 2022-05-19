using Xunit;
using Microsoft.CodeAnalysis;
using Codelyzer.Analysis.Common;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Codelyzer.Analysis.CSharp;
using Microsoft.CodeAnalysis.CSharp;
using Codelyzer.Analysis.Model;

namespace Codelyzer.Analysis.Languages.UnitTests
{
    public class CSharpHandlerTests
    {

		private AnalyzerConfiguration Configuration;
		private readonly ITestOutputHelper _output;

		public CSharpHandlerTests(ITestOutputHelper output)
		{
			_output = output;
			Configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
			{
				ExportSettings =
				{
					GenerateJsonOutput = false,
				},

				MetaDataSettings =
				{
					LiteralExpressions = true,
					MethodInvocations = true,
					Annotations = true
				
				}
			};
		}

		[Fact]
		public void ClassHandlerTest()
		{
			const string classSnippet = @"
				public class Person
				{
					public string Name { get; set; }
					public int Age { get; set; }

					public Person()
					{
						Console.WriteLine(""This is a constructor"");
					}

					public void SetName(string newName)
					{
						Name = newName;
					}

					public string GetName()
					{
						return Name;
					}
				}";
			//ClassNode
			var rootNode = GetCSharpUstNode(classSnippet);
			Assert.Single(rootNode.Children);
			var classNode = rootNode.Children[0];
			Assert.Equal(typeof(Model.ClassDeclaration), classNode.GetType());
			Assert.Equal(3, classNode.Children.Count);
			//Child 0: construction
			var constructionNode  = classNode.Children[0];
			Assert.Equal(typeof(Model.ConstructorDeclaration), constructionNode.GetType());
			Assert.Single(constructionNode.Children);
			//construction: block
			var blockNode = constructionNode.Children[0];
			Assert.Equal(typeof(Model.BlockStatement), blockNode.GetType());
			Assert.Single(blockNode.Children);
			//construction: invocationExpress
			var invocationNode = blockNode.Children[0];
			Assert.Equal(typeof(Model.InvocationExpression), invocationNode.GetType());
			//construction: invocationExpress: Arguments
			var arguments = ((InvocationExpression)invocationNode).Arguments;
			Assert.Single(arguments);
			//construction: invocationExpress: InvocationExpression
			Assert.Single(((InvocationExpression)invocationNode).Children);
			var literalExpressionNode = ((InvocationExpression)invocationNode).Children[0];
			Assert.Equal(typeof(Model.LiteralExpression), literalExpressionNode.GetType());

			//Child 1: MethodDeclaration - SetName
			var method1Node = classNode.Children[1];
			Assert.Equal("SetName", method1Node.Identifier);
			Assert.Equal(typeof(Model.MethodDeclaration), method1Node.GetType());
			var method1Parameters = ((MethodDeclaration)method1Node).Parameters;
			Assert.Single(method1Parameters);
			Assert.Equal("void", ((MethodDeclaration)method1Node).ReturnType);
			//Child 2: MethodDeclaration - GetName
			var method2Node = classNode.Children[2];
			Assert.Equal("GetName", method2Node.Identifier);
			Assert.Equal(typeof(Model.MethodDeclaration), method2Node.GetType());
			var method2Parameters = ((MethodDeclaration)method2Node).Parameters;
			Assert.True(method2Parameters.Count == 0);
			Assert.Equal("string", ((MethodDeclaration)method2Node).ReturnType);
		}

		[Fact]
		public void InvocationExpressionHandlerTest()
		{
			const string codeSnippet =
			@"class TypeName
			{
				public void Test()
				{
					var a = ""test"";
					string.Equals(a, ""v"", System.StringComparison.Ordinal);
				}
			}";
			var rootNode = GetCSharpUstNode(codeSnippet);
			Assert.Single(rootNode.Children);
			var classNode = rootNode.Children[0];
			Assert.True(classNode.GetType() == typeof(Model.ClassDeclaration));
			Assert.Single(classNode.Children);

			var methodNode = classNode.Children[0];
			Assert.True(methodNode.GetType() == typeof(Model.MethodDeclaration));
			Assert.Single(methodNode.Children);

			var blockNode = methodNode.Children[0];
			Assert.True(blockNode.GetType() == typeof(Model.BlockStatement));
			Assert.Equal(2, blockNode.Children.Count);

			var literalNode = blockNode.Children[0];
			Assert.True(literalNode.GetType() == typeof(Model.LiteralExpression));
			Assert.Equal("test", literalNode.Identifier);


			var invocationNode = blockNode.Children[1];
			Assert.True(invocationNode.GetType() == typeof(Model.InvocationExpression));
			var invocationExpressionNode = (InvocationExpression)invocationNode;
			Assert.Equal("Equals", invocationExpressionNode.MethodName);

			Assert.Equal(3, invocationExpressionNode.Arguments.Count);
			Assert.Equal("a", invocationExpressionNode.Arguments[0].Identifier);
		}
		[Fact]
		public void AttributeHandlerTest()
		{
			var expressShell = @"
				[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
				public class AuthorAttribute : Attribute
				{
					private string name;
					public AuthorAttribute(string name)
					{
						this.name = name;
					}
					public string Name
					{
						get { return name; }
					}
				}";
			var rootNode = GetCSharpUstNode(expressShell);
			Assert.Single(rootNode.Children);
			var classNode = rootNode.Children[0];
			var annotationNode = classNode.Children[0];
            Assert.Equal(typeof(Model.Annotation), annotationNode.GetType());
			Assert.Equal(2, annotationNode.Children.Count);
			Assert.Equal("AttributeTargets.Class", annotationNode.Children[0].Identifier);
			Assert.Equal("AllowMultiple = true", annotationNode.Children[1].Identifier);

			var constructionNode = rootNode.Children[1];
			Assert.Equal(typeof(Model.ConstructorDeclaration), constructionNode.GetType());
			Assert.Equal("AuthorAttribute", constructionNode.Identifier);

			var returnNode = rootNode.Children[2];
			Assert.Equal(typeof(Model.ReturnStatement), returnNode.GetType());
			Assert.Equal("name", returnNode.Identifier);
		}

        private Model.UstNode GetCSharpUstNode(string expressionShell)
		{
			var tree = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(expressionShell);
			var compilation = CSharpCompilation.Create(
				"test.dll",
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
				syntaxTrees: new[] { tree },
				references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

			// Check for errors
			//var diagnostics = compilation.GetDiagnostics();

			CodeContext codeContext = new CodeContext(null, null, tree, null, null, Configuration, new NullLogger<CSharpRoslynProcessor>());
			CSharpRoslynProcessor processor = new CSharpRoslynProcessor(codeContext);

			var result = processor.Visit(codeContext.SyntaxTree.GetRoot());
			return result;
		}


	}
}
