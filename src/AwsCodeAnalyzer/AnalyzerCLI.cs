using System;
using System.Collections.Generic;
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
        
        [Option('j', "json-input", Required = true, HelpText = "Configuration json input.")]
        public string JsonInput { get; set; }
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

                    Configuration = SerializeUtils.FromJson<AnalyzerConfiguration>(o.JsonInput);
                });
        }
        
        static void HandleParseError(IEnumerable<Error> errs)
        {
            Environment.Exit( -1 );
        }
    }
    
    
}