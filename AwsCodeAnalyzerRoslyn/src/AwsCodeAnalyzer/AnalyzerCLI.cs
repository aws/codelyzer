using System;
using System.Collections.Generic;
using System.IO;
using AwsCodeAnalyzer.Common;
using CommandLine;

namespace AwsCodeAnalyzer
{
    public class Options
    {
        [Option('p', "project-path", Required = true, HelpText = "Project file path.")]
        public string ProjectPath { get; set; }
        
        [Option('s', "solution-path", Required = false, HelpText = "Solution file path.")]
        public string SolutionPath { get; set; }
        
        [Option('j', "json-input", Required = false, HelpText = "Configuration json input.")]
        public string JsonInput { get; set; }

        [Option('f', "json-input-infile", Required = true, HelpText = "Configuration json input file")]
        public string JsonInputFile { get; set; }
    }
    
    public class AnalyzerCLI
    {
        public bool Project;
        public string FilePath;
        public AnalyzerConfiguration Configuration;

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

                    String jsonData;
                    if (o.JsonInputFile != null && o.JsonInputFile.Length != 0)
                    {
                        try
                        {
                            jsonData = File.ReadAllText(o.JsonInputFile);
                        }
                        catch (Exception e)
                        {
                            jsonData = null;
                            Console.WriteLine("Exception " + e.Message);
                            Environment.Exit(-1);
                        }
                    } else
                    {
                        jsonData = o.JsonInput;
                    }
                    Configuration = SerializeUtils.FromJson<AnalyzerConfiguration>(jsonData);
                });
        }
        
        static void HandleParseError(IEnumerable<Error> errs)
        {
            Environment.Exit( -1 );
        }
    }
    
    
}