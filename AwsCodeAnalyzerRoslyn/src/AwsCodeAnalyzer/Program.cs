using System;
using System.IO;
using System.Linq;
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
            AnalyzerCLI cli = new AnalyzerCLI();
            cli.HandleCommand(args);
            Console.WriteLine(cli.Project + " -- " + cli.FilePath);
            Console.WriteLine(SerializeUtils.ToJson(cli.Configuration));


            /* 1. Logger object */
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            /* 2. Create Configuration settings */
            /*AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                ExportSettings =
                {
                    GenerateJsonOutput = true,
                    OutputPath = outputPath
                },

                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true
                }
            };*/

            /* 3. Get Analyzer instance based on language */
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(cli.Configuration, Log.Logger);

            /* 4. Analyze the project or solution */
            var analyzerResult = await analyzer.AnalyzeProject(cli.FilePath);
            Console.WriteLine("Exported to : " + analyzerResult.OutputJsonFilePath);

            /* Consume the results as model objects */
            var sourcefile = analyzerResult.ProjectResult.SourceFileResults.First();
            foreach (var invocation in sourcefile.AllInvocationExpressions())
            {
                Console.WriteLine(invocation.MethodName + ":" + invocation.SemanticMethodSignature);
            }

            var objectCreations = sourcefile.AllObjectCreationExpressions();
            var allClasses = sourcefile.AllClasses();
            var allMethods = sourcefile.AllMethods();
            var allLiterals = sourcefile.AllLiterals();

            // Verify the exported file
            /*string exportJsonFile = analyzerResult.OutputJsonFilePath;
            string newFile = Path.GetFileName(projectPath) + ".export";
            var exportedProject = SerializeUtils.FromJson<ProjectWorkspace>(FileUtils.ReadFile(exportJsonFile));
            string exportedJson = SerializeUtils.ToJson(exportedProject);
            var exportResult = await FileUtils.WriteFileAsync(
                configuration.ExportSettings.OutputPath, newFile,
                exportedJson);
            Console.WriteLine("Re-exported to : " + exportResult);*/
        }
    }
}
    
