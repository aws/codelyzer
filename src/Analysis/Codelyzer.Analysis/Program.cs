using Codelyzer.Analysis.Model;
using JetBrains.Profiler.Api;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Codelyzer.Analysis
{
    [ExcludeFromCodeCoverage]
    class Program
    {
        static async Task Main(string[] args)
        {
            var memory = GC.GetTotalMemory(false);
            AnalyzerCLI cli = new AnalyzerCLI();
            cli.HandleCommand(args);
            Console.WriteLine(cli);

            /* 1. Logger object */
            var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddConsole());


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
            cli.Configuration.MetaDataSettings.DeclarationNodes = true;
            cli.Configuration.MetaDataSettings.ReferenceData = true;

            /* 3. Get Analyzer instance based on language */
            CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(cli.Configuration, 
                loggerFactory.CreateLogger("Analyzer"));


            /* 4. Analyze the project or solution */
            AnalyzerResult analyzerResult = null;
            if (cli.Project)
            {
                analyzerResult = await analyzer.AnalyzeProject(cli.FilePath);
                if (analyzerResult.OutputJsonFilePath != null)
                {
                    Console.WriteLine("Exported to : " + analyzerResult.OutputJsonFilePath);
                }
            }
            else
            {
                var analyzerResults = await analyzer.AnalyzeSolution(cli.FilePath);
                foreach (var aresult in analyzerResults)
                {
                    if (aresult.OutputJsonFilePath != null)
                    {
                        Console.WriteLine("Exported to : " + aresult.OutputJsonFilePath);
                    }
                }

                if (analyzerResults.Count > 0)
                {
                    analyzerResult = analyzerResults[0];
                }
            }

            /* Consume the results as model objects */
            var sourcefile = analyzerResult.ProjectResult.SourceFileResults.First();
            foreach (var invocation in sourcefile.AllInvocationExpressions())
            {
                Console.WriteLine(invocation.MethodName + ":" + invocation.SemanticMethodSignature);
            }
        }
    }
}
    
