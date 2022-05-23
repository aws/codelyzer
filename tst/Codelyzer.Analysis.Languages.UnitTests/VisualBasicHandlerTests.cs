using Xunit;
using Microsoft.CodeAnalysis;
using Codelyzer.Analysis.Common;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Codelyzer.Analysis.VisualBasic;
using System.Linq;

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
					DeclarationNodes = true,
					LambdaMethods = true,
					InterfaceDeclarations = true
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
			var classBlockNode = rootNode.Children[0];
			Assert.Single(classBlockNode.Children.Where(c=> c.GetType() == typeof(Model.ClassStatement)));
			var classStatementNode = classBlockNode.Children.Single(c => c.GetType() == typeof(Model.ClassStatement));
			Assert.Single(classStatementNode.Children);
			var attributeListNode = classStatementNode.Children[0];
			Assert.Equal(typeof(Model.AttributeList), attributeListNode.GetType());
			Assert.Single(attributeListNode.Children);
			var argumentListNode = attributeListNode.Children[0];
			Assert.Equal(typeof(Model.ArgumentList), argumentListNode.GetType());
			Assert.Equal(2, argumentListNode.Children.Count);
			var attribute1 = (Model.AttributeArgument)argumentListNode.Children.FirstOrDefault();
			var attribute2 = (Model.AttributeArgument)argumentListNode.Children.LastOrDefault();
			Assert.Equal("AttributeTargets.[Class]", attribute1.ArgumentExpression);
			Assert.Equal("True", attribute2.ArgumentExpression);
			Assert.Equal("AllowMultiple", attribute2.ArgumentName);
		}


		[Fact]
		public void Function1HandlerTest()
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
			Assert.Single(((Model.MethodStatement)subStatementNode).Parameters);
			var p1 = ((Model.MethodStatement)subStatementNode).Parameters[0];
			Assert.Equal("HttpContext", p1.Type);
			
			var returnNode = funcBlockNode.Children[1];
			Assert.True(returnNode.GetType() == typeof(Model.ReturnStatement));
			Assert.Single(returnNode.Children);
			var literalNode = returnNode.Children[0];
			Assert.True(literalNode.GetType() == typeof(Model.LiteralExpression));
			var endBlockNode = funcBlockNode.Children[2];
			Assert.True(endBlockNode.GetType() == typeof(Model.EndBlockStatement));
			Assert.Equal("End Function", endBlockNode.Identifier);
		}

		[Fact]
		public void Function2HandlerTest()
		{
			var expressShell = @"
			Function CreatePerson(ByVal value As String) As Person
				Return New Person()
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
			Assert.Single(((Model.MethodStatement)subStatementNode).Parameters);
			var p1 = ((Model.MethodStatement)subStatementNode).Parameters[0];
			Assert.Equal("String", p1.Type);

			var subStatementTypeNode = subStatementNode.Children.LastOrDefault();
			Assert.Equal("Person", ((Model.SimpleAsClause)subStatementTypeNode).Type); 

			var returnNode = funcBlockNode.Children[1];
			Assert.True(returnNode.GetType() == typeof(Model.ReturnStatement));
			Assert.Single(returnNode.Children);
			
			var endBlockNode = funcBlockNode.Children[2];
			Assert.True(endBlockNode.GetType() == typeof(Model.EndBlockStatement));
			Assert.Equal("End Function", endBlockNode.Identifier);
		}

		[Fact]
		public void ConstructionHandlerTest()
		{
			var expressShell = @"
			Sub New(isReusable As Boolean)
				Me.IsReusable = isReusable
			End Sub";
			var rootNode = GetVisualBasicUstNode(expressShell);
			Assert.Single(rootNode.Children);
			var constructionNode = rootNode.Children[0];
			Assert.True(constructionNode.GetType() == typeof(Model.ConstructorBlock));
			Assert.Single(((Model.ConstructorBlock)constructionNode).Parameters);
			var p = ((Model.ConstructorBlock)constructionNode).Parameters[0];
			Assert.Equal("isReusable", p.Name);
			Assert.Equal("Boolean", p.Type);
		}

		[Fact]
		public void LambdaHandlerTest()
		{
			var expressShell = @"
			Dim increment1 = Function(x) x + 1
			Dim increment2 = Function(x)
                     Return x + 2
            End Function";

			var rootNode = GetVisualBasicUstNode(expressShell);
			Assert.Equal(2, rootNode.Children.Count);
			var declare1Node = rootNode.Children[0];
			Assert.True(declare1Node.GetType() == typeof(Model.FieldDeclaration));
			var variable1Node = declare1Node.Children[0];
			Assert.True(variable1Node.GetType() == typeof(Model.VariableDeclarator));
			var singleLambdaNode = variable1Node.Children[0];
			Assert.True(singleLambdaNode.GetType() == typeof(Model.SingleLineLambdaExpression));

			var declare2Node = rootNode.Children[1];
			Assert.True(declare2Node.GetType() == typeof(Model.FieldDeclaration));
			var variable2Node = declare2Node.Children[0];
			Assert.True(variable2Node.GetType() == typeof(Model.VariableDeclarator));
			var multiLambdaNode = variable2Node.Children[0];
			Assert.True(multiLambdaNode.GetType() == typeof(Model.MultiLineLambdaExpression));
		}

		[Fact]
		public void PropertyHandlerTest()
		{
			var expressShell = @"
			Public ReadOnly Property Url() As String
				Get
					Return UrlValue
				End Get
			End Property";
			var rootNode = GetVisualBasicUstNode(expressShell);
			Assert.Single(rootNode.Children);
			var propertyBlockNode = rootNode.Children[0];
			Assert.True(propertyBlockNode.GetType() == typeof(Model.PropertyBlock));
			Assert.Equal("PropertyBlock", propertyBlockNode.Identifier);
			Assert.Equal(3, propertyBlockNode.Children.Count);
			
			var propertyStatementNode = propertyBlockNode.Children[0];
			Assert.True(propertyStatementNode.GetType() == typeof(Model.PropertyStatement));
			Assert.Single(propertyStatementNode.Children);
			var asTypeNode = propertyStatementNode.Children[0];
			Assert.True(asTypeNode.GetType() == typeof(Model.SimpleAsClause));
			Assert.Equal("String", ((Model.SimpleAsClause)asTypeNode).Type);

			var accessorBlockNode = propertyBlockNode.Children[1];
			Assert.True(accessorBlockNode.GetType() == typeof(Model.AccessorBlock));
			Assert.Equal(3, accessorBlockNode.Children.Count);

			var accessorStatementNode = accessorBlockNode.Children[0];
			Assert.True(accessorStatementNode.GetType() == typeof(Model.AccessorStatement));
			var returnStatementNode = accessorBlockNode.Children[1];
			Assert.True(returnStatementNode.GetType() == typeof(Model.ReturnStatement));
			var endStatementNode = accessorBlockNode.Children[2];
			Assert.True(endStatementNode.GetType() == typeof(Model.EndBlockStatement));
			Assert.Equal("End Get", endStatementNode.Identifier);

			var endBlockNode = propertyBlockNode.Children[2];
			Assert.True(endBlockNode.GetType() == typeof(Model.EndBlockStatement));
			Assert.Equal("End Property", endBlockNode.Identifier);
		}

		[Fact]
		public void ModuleHandlerTest()
		{
			var expressShell = @"
			Module Module1
			End Module";
			var rootNode = GetVisualBasicUstNode(expressShell);
			Assert.Single(rootNode.Children);
			var moduleNode = rootNode.Children[0];

			Assert.True(moduleNode.GetType() == typeof(Model.ModuleBlock));
			Assert.True(moduleNode.Children[0].GetType() == typeof(Model.ModuleStatement));
		}

		[Fact]
		public void EnumHandlerTest()
		{
			var expressShell = @"
			Private Enum SampleEnum
				SampleMember
			End Enum";
			var rootNode = GetVisualBasicUstNode(expressShell);
			Assert.Single(rootNode.Children);

			var enumBlockNode = rootNode.Children[0];
			Assert.True(enumBlockNode.GetType() == typeof(Model.EnumBlock));
			Assert.Equal("enumBlock", enumBlockNode.Identifier);
			Assert.Equal(3, enumBlockNode.Children.Count);

			var enumStatementNode = enumBlockNode.Children[0];
			Assert.True(enumStatementNode.GetType() == typeof(Model.EnumStatement));

			var enumMemberDeclarationNode = enumBlockNode.Children[1];
			Assert.True(enumMemberDeclarationNode.GetType() == typeof(Model.EnumMemberDeclaration));

			var endBlockNode = enumBlockNode.Children[2];
			Assert.True(endBlockNode.GetType() == typeof(Model.EndBlockStatement));
			Assert.Equal("End Enum", endBlockNode.Identifier);
		}

		[Fact]
		public void ObjectCreationHandlerTest()
		{
			var expressShell = @"
			Sub Main()
				Dim request As New HttpRequest("""", ""http://localhost"", """")
			End Sub";

			var rootNode = GetVisualBasicUstNode(expressShell);
			Assert.Single(rootNode.Children);
			var methodBlockNode = rootNode.Children[0];
			Assert.True(methodBlockNode.GetType() == typeof(Model.MethodBlock));

			var methodStatementNode = methodBlockNode.Children[0];
			Assert.True(methodStatementNode.GetType() == typeof(Model.MethodStatement));
			var localDeclarationNode = methodBlockNode.Children[1];
			Assert.True(localDeclarationNode.GetType() == typeof(Model.LocalDeclarationStatement));
			var variableNode = localDeclarationNode.Children[0];
			Assert.True(variableNode.GetType() == typeof(Model.VariableDeclarator));
			var objectCreateNode = variableNode.Children[0];
			Assert.True(objectCreateNode.GetType() == typeof(Model.ObjectCreationExpression));
			Assert.Equal(3, ((Model.ObjectCreationExpression)objectCreateNode).Arguments.Count);

			var endNode = methodBlockNode.Children[2];
			Assert.True(endNode.GetType() == typeof(Model.EndBlockStatement));

		}

		[Fact]
		public void NameSpaceHandlerTest()
		{
			var expressShell = @"
			Namespace System.Collections.Generic
				Class specialSortedList(Of T)
					Inherits List(Of T)
					' Insert code to define the special generic list class.

				End Class
			End Namespace";

			var rootNode = GetVisualBasicUstNode(expressShell);
			Assert.Single(rootNode.Children);
			var nsblockNode = rootNode.Children[0];
			Assert.True(nsblockNode.GetType() == typeof(Model.NamespaceBlock));

			var nsStatementNode = nsblockNode.Children[0];
			Assert.True(nsStatementNode.GetType() == typeof(Model.NamespaceStatement));
			Assert.Equal(3, nsStatementNode.Children.Count);
			Assert.Equal("System", nsStatementNode.Children[0].Identifier);
			Assert.Equal("Collections", nsStatementNode.Children[1].Identifier);
			Assert.Equal("Generic", nsStatementNode.Children[2].Identifier);
		}


		[Fact]
		public void InterfaceHandlerTest()
		{
			var expressShell = @"
				Namespace CsharpInterface
				Interface IPolygon
					Sub calculateArea(ByVal l As Integer, ByVal b As Integer)
				End Interface

				Class Rectangle
					Implements IPolygon

					Public Sub calculateArea(ByVal l As Integer, ByVal b As Integer)
						Dim area As Integer = l * b
						Console.WriteLine(""Area of Rectangle: "" & area)
					End Sub
				End Class
			End Namespace";
			var rootNode = GetVisualBasicUstNode(expressShell);
			Assert.Single(rootNode.Children);
			var interfaceNode = rootNode.Children[0].Children[1];
			Assert.Equal(typeof(Model.InterfaceBlock), interfaceNode.GetType());
			Assert.Equal(3, interfaceNode.Children.Count);
			var interfaceStatementNode = interfaceNode.Children[0];
			Assert.Equal(typeof(Model.InterfaceStatement), interfaceStatementNode.GetType());
			
			var methodStatementNode = interfaceNode.Children[1];
			Assert.Equal(typeof(Model.MethodStatement), methodStatementNode.GetType());

			Assert.Equal(2, ((Model.MethodStatement)methodStatementNode).Parameters.Count);
			var p1 = ((Model.MethodStatement)methodStatementNode).Parameters[0];
			Assert.Equal("l", p1.Name);
			Assert.Equal("Integer", p1.Type);
			var endNode = interfaceNode.Children[2];
			Assert.Equal(typeof(Model.EndBlockStatement), endNode.GetType());

			
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
