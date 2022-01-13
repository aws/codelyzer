using System;
using System.Collections.Generic;
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

        [Option('b', "bin-files", Required = false, HelpText = "Generate Bin Files")]
        public string GenerateBinFiles { get; set; }

        [Option('t', "max-threads", Required = false, HelpText = "Number of concurrent threads")]
        public string ConcurrentThreads { get; set; }

        [Option('l', "location-data", Required = false, HelpText = "Add location data to the result")]
        public string LocationData { get; set; }

        [Option('r', "reference-data", Required = false, HelpText = "Add reference data to the result")]
        public string ReferenceData { get; set; }

        [Option('d', "detailed-analysis", Required = false, HelpText = "Run detailed analysis that includes enums, structs, interfaces, annotations, and declaration nodes")]
        public string DetailedAnalysis { get; set; }

        [Option('f', "analyze-failed", Required = false, HelpText = "Analyze projects that fail design build")]
        public string AnalyzeFailed { get; set; }

        [Option('m', "meta-data", Required = false, HelpText = "metadata string, e:g: 'EnumDeclarations=true,StructDeclarations=true,Annotations=true'")]
        public string MetaDataDetails { get; set; }

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

                    if (!string.IsNullOrEmpty(o.GenerateBinFiles))
                    {
                        Configuration.MetaDataSettings.GenerateBinFiles = o.GenerateBinFiles.ToLower() == bool.TrueString.ToLower();
                    }

                    if (!string.IsNullOrEmpty(o.ReferenceData))
                    {
                        Configuration.MetaDataSettings.ReferenceData = o.ReferenceData.ToLower() == bool.TrueString.ToLower();
                    }

                    if (!string.IsNullOrEmpty(o.LocationData))
                    {
                        Configuration.MetaDataSettings.LocationData = o.LocationData.ToLower() == bool.TrueString.ToLower();
                    }

                    if (!string.IsNullOrEmpty(o.DetailedAnalysis))
                    {
                        var result = o.DetailedAnalysis.ToLower() == bool.TrueString.ToLower();

                        Configuration.MetaDataSettings.EnumDeclarations = result;
                        Configuration.MetaDataSettings.StructDeclarations = result;
                        Configuration.MetaDataSettings.InterfaceDeclarations = result;
                        Configuration.MetaDataSettings.DeclarationNodes = result;
                        Configuration.MetaDataSettings.Annotations = result;
                        Configuration.MetaDataSettings.ElementAccess = result;
                        Configuration.MetaDataSettings.MemberAccess = result;
                    }
                    else if (!string.IsNullOrEmpty(o.MetaDataDetails))
                    {
                        ConfigureMetaDataDetails(o.MetaDataDetails, Configuration.MetaDataSettings);
                    }

                    if (!string.IsNullOrEmpty(o.ConcurrentThreads))
                    {
                        int.TryParse(o.ConcurrentThreads, out int concurrentThreads);
                        Configuration.ConcurrentThreads = concurrentThreads > 0 ? concurrentThreads : Constants.DefaultConcurrentThreads;
                    }

                    if (!string.IsNullOrEmpty(o.AnalyzeFailed))
                    {
                        Configuration.AnalyzeFailedProjects = o.AnalyzeFailed.ToLower() == bool.TrueString.ToLower();
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
            Environment.Exit(-1);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"File Path: {FilePath};");
            sb.Append($"\nConfiguration: {SerializeUtils.ToJson(Configuration)}");
            return sb.ToString();
        }

        private void ConfigureMetaDataDetails(string metaDataDetails, MetaDataSettings metaDataSettings)
        {
            var metaDatas = metaDataDetails.Split(",");
            Type MDType = typeof(MetaDataSettings);
            foreach (string md in metaDatas)
            {
                string[] eachEntry = md.Split("=");
                if (eachEntry != null && eachEntry.Length > 1)
                {
                    string metadataField = eachEntry[0].Trim();
                    bool metadataVal = eachEntry[1].Trim().Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? true : false;
                    MDType.GetField(metadataField)?.SetValue(metaDataSettings, metadataVal);
                }
            }
        }
    }


}
