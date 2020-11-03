using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Codelyzer.Analysis.Common;
using CommandLine;

namespace Codelyzer.Analysis
{
    public class Options
    {
        [Option('p', "project-path", Required = false, HelpText = "Project file path.")]
        public string ProjectPath { get; set; }
        
        [Option('s', "solution-path", Required = false, HelpText = "Solution file path.")]
        public string SolutionPath { get; set; }

        [Option('o', "json-output-path", Required = false, HelpText = "Json output file path")]
        public string JsonFilePath { get; set; }
    }
    
    public class AnalyzerCLI
    {
        public bool Project;
        public string FilePath;
        public AnalyzerConfiguration Configuration;

        public AnalyzerCLI()
        {
            Configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
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

        public void HandleCommand(String[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithNotParsed(HandleParseError)
                .WithParsed<Options>(o =>
                {
                    if (!string.IsNullOrEmpty(o.ProjectPath))
                    {
                        Project = true;
                        FilePath = o.ProjectPath;
                    }
                    else
                    {
                        Project = false;
                        FilePath = o.SolutionPath;
                    }

                    if (!string.IsNullOrEmpty(o.JsonFilePath))
                    {
                        Configuration.ExportSettings.GenerateJsonOutput = true;
                        Configuration.ExportSettings.OutputPath = o.JsonFilePath;
                    }
                });

            if (FilePath == null)
            {
                Console.WriteLine("Project or Solution File path is missing");
                Environment.Exit(-1);
            }
        }
        
        static void HandleParseError(IEnumerable<Error> errs)
        {
            Environment.Exit( -1 );
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"File Path: {FilePath};");
            sb.Append($"\nConfiguration: {SerializeUtils.ToJson(Configuration)}");
            return sb.ToString();
        }
    }
    
    
}
