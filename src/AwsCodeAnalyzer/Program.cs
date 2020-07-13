using System;
using System.IO;
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
            var analyzerResult = await analyzer.AnalyzeProject(path);
            Console.WriteLine("Exported to : " + analyzerResult.OutputJsonFilePath);
            
            // Verify the exported file
            string exportJsonFile = analyzerResult.OutputJsonFilePath;
            string newFile = Path.GetFileName(exportJsonFile) + ".export";
            var exportedProject = SerializeUtils.FromJson<ProjectWorkspace>(FileUtils.ReadFile(exportJsonFile));
            string exportedJson = SerializeUtils.ToJson(exportedProject);
            var exportResult = await FileUtils.WriteFileAsync(options.JsonOutputPath, newFile, exportedJson);
            Console.WriteLine("Re-exported to : " + exportResult);
        }
    }
}
    
