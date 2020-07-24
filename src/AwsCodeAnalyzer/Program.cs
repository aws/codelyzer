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
            if (args == null || args.Length != 2)
            {
                Console.WriteLine("Missing Arguments. <ProjectFilePath> <OutputFilePath>");
                return;
            }
            
            string projectPath = args[0];
            string outputPath = args[1];

            Console.WriteLine("Project file: " + projectPath);
            Console.WriteLine("Output path: " + outputPath);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
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
            };
            
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, Log.Logger);
            var analyzerResult = await analyzer.AnalyzeProject(projectPath);
            Console.WriteLine("Exported to : " + analyzerResult.OutputJsonFilePath);
            var sourcefile = analyzerResult.ProjectResult.SourceFileResults.First();
            foreach (var invocation in sourcefile.AllInvocationExpressions())
            {
                Console.WriteLine(invocation.MethodName + ":" + invocation.SemanticMethodSignature);
            }

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
    
