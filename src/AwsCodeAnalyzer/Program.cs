using System;
using System.Threading.Tasks;
using AwsCodeAnalyzer.Build;
using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.CSharp;
using AwsCodeAnalyzer.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace AwsCodeAnalyzer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string path = 
            path = "/Users/shiramsn/workplace/encore/src/AmazonSourceCodeAnalyzerRoslyn/AmazonSourceCodeAnalyzerRoslyn.csproj";
           
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            AnalyzerOptions options = new AnalyzerOptions(AnalyzerOptions.LANGUAGE_CSHARP);
            options.JsonOutputPath = @"/Users/shiramsn/encore/analysis";
            
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(options, Log.Logger);
            var result = await analyzer.AnalyzeProject(path);

        }
        static async Task ___Main(string[] args)
        {
            string path = "/Users/shiramsn/workplace/encore/src/NugetCompatStatistics/packages/csharp/ApiHandler/src/AwsEncoreServiceCache/AwsEncoreServiceCache.csproj";
            path = "/Users/shiramsn/workplace/encore/src/AmazonSourceCodeModel/AmazonSourceCodeModel.csproj";
            path = "/Users/shiramsn/workplace/encore/src/AmazonSourceCodeAnalyzerRoslyn/AmazonSourceCodeAnalyzerRoslyn.csproj";
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            
            
            Log.Logger.Debug("Starting ...");
            WorkspaceBuilder builder = new WorkspaceBuilder(Log.Logger, path);
            var projectResults = await builder.Build();
            foreach (var projectResult in projectResults)
            {
                ProjectWorkspace workspace = new ProjectWorkspace(projectResult.ProjectPath)
                {
                    SourceFiles = projectResult.SourceFiles,
                    BuildErrors = projectResult.BuildErrors,
                    BuildErrorsCount = projectResult.BuildErrors.Count
                };

                foreach (var fileBuildResult in projectResult.SourceFileBuildResults)
                {
                    if (!fileBuildResult.SourceFileFullPath.Contains("CSharpCoreAnalyzer.cs")) continue;
                    
                    CodeContext codeContext = new CodeContext(fileBuildResult.SemanticModel, 
                        fileBuildResult.SyntaxTree, 
                        workspace.ProjectRootPath,
                        fileBuildResult.SourceFilePath,
                        Log.Logger);
                    Log.Logger.Debug("Analyzing: " + fileBuildResult.SourceFileFullPath);
                    CSharpRoslynProcessor processor = new CSharpRoslynProcessor(codeContext);
                    var result = processor.Visit(codeContext.SyntaxTree.GetRoot());
                    workspace.SourceFileResults.Add(result);
                }
                
                var output = SerializeUtils.ToJson<ProjectWorkspace>(workspace);
                var outfile = await FileUtils.WriteFileAsync("/tmp", workspace.ProjectName+".json", output);
                Log.Logger.Debug(outfile);
            }
        }
        
        static void _Main(string[] args)
        {
            //A syntax tree with an unnecessary semicolon on its own line
            var tree = CSharpSyntaxTree.ParseText(@"
    public class Sample
    {
       public static void Foo(int param1, string param2, object param3)
       {
          Console.WriteLine();
          String arg = ""arg"";
          int a = 100;
          char e = 'c';
          Console.WriteLine(String.Format(""ddddd ""));
        }
    }");
            
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var compilation = CSharpCompilation.Create("HelloWorld")
                .AddReferences(MetadataReference.CreateFromFile(
                    typeof(string).Assembly.Location))
                .AddSyntaxTrees(tree);

            SemanticModel model = compilation.GetSemanticModel(tree);

            CodeContext context = new CodeContext(model, tree, "", "", Logger.None);
            CSharpRoslynProcessor processor = new CSharpRoslynProcessor(context);
            var result = processor.Visit(tree.GetRoot());
            var output =Serialize.ToJson(result);
            Console.WriteLine(output);
        }
    }
}
    
