using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using AwsCodeAnalyzer.Common;
using Buildalyzer;
using Buildalyzer.Environment;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Serilog;

namespace AwsCodeAnalyzer.Build
{
    public class WorkspaceBuilderHelper
    {
        private const string TargetFramework = nameof(TargetFramework);
        private const string TargetFrameworkVersion = nameof(TargetFrameworkVersion);
        private const string Configuration = nameof(Configuration);



        public readonly List<Project> Projects = null;
        private ILogger Logger { get; set; }
        
        public WorkspaceBuilderHelper(ILogger logger, string workspacePath)
        {
            this.Logger = logger;
            this.WorkspacePath = workspacePath;
            this.Projects = new List<Project>();
        }

        private string WorkspacePath { get; }
        
        private bool IsSolutionFile()
        {
            return WorkspacePath.EndsWith("sln");
        }

        public void Build()
        {
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);
           
           /* Uncomment the below code to debug issues with msbuild */
            /*var writer = new StreamWriter(Console.OpenStandardOutput());
            writer.AutoFlush = true;

            Console.SetOut(writer);
            Console.SetError(writer);*/

            if (IsSolutionFile())
            {
                Logger.Information("Loading the Workspace (Solution): " + WorkspacePath);

                AnalyzerManager analyzerManager = new AnalyzerManager(WorkspacePath,
                   new AnalyzerManagerOptions
                   {
                       LogWriter = writer
                   });
                
                Logger.Information("Loading the Solution Done: " + WorkspacePath);

                // AnalyzerManager builds the projects based on their dependencies
                // After this, code does not depend on Buildalyzer
                var projects = BuildSolution(analyzerManager).CurrentSolution.Projects;
                Projects.AddRange(projects);
            } else
            {
                AnalyzerManager analyzerManager = new AnalyzerManager(new AnalyzerManagerOptions
                {
                    LogWriter = writer
                });
                AdhocWorkspace workspace = new AdhocWorkspace();
                Queue<string> queue = new Queue<string>();
                ISet<string> existing = new HashSet<string>();
                
                queue.Enqueue(WorkspacePath);
                existing.Add(WorkspacePath);

                /*
                 * We need to resolve all the project dependencies to avoid compilation errors.
                 * If we have compilation errors, we might miss some of the semantic values.
                 */
                while (queue.Count > 0)
                {
                    var path = queue.Dequeue();
                    Logger.Information("Building: " + path);
                    
                    IProjectAnalyzer projectAnalyzer = analyzerManager.GetProject(path);
                    IAnalyzerResults analyzerResults = projectAnalyzer.Build(GetEnvironmentOptions());
                    IAnalyzerResult analyzerResult = analyzerResults.First();
                    analyzerResult.AddToWorkspace(workspace);
                    foreach (var pref in analyzerResult.ProjectReferences)
                    {
                        if (!existing.Contains(pref))
                        {
                            existing.Add(pref);
                            queue.Enqueue(pref);
                        }
                    }
                }
                
                var project = workspace.CurrentSolution.Projects.First(p => p.FilePath.Equals(WorkspacePath));
                Projects.Add(project);
               
            }

            Logger.Debug(sb.ToString());
            writer.Close();
        }

        /*
         *   Build all the projects in workspace 
         *   TODO: Need to handle different type of projects like VB, CSharp, etc.,
         *   TODO: Fix needed from Buildalyzer: https://github.com/daveaglick/Buildalyzer/issues/113
         * */
        private AdhocWorkspace BuildSolution(IAnalyzerManager manager)
        {
            // Run builds in parallel
            List<IAnalyzerResult> results = manager.Projects.Values
                .AsParallel()
                .Select(p => {
                    Logger.Debug("Building the project : " + p.ProjectFile.Path);
                    return BuildProject(p);
                })
                .Where(x => x != null)
                .ToList();

            // Add each result to a new workspace
            AdhocWorkspace workspace = new AdhocWorkspace();
            foreach (IAnalyzerResult result in results)
            {
                result.AddToWorkspace(workspace);
            }
            return workspace;
        }

        private IAnalyzerResult BuildProject(IProjectAnalyzer projectAnalyzer)
        {
            try
            {
                return projectAnalyzer.Build(GetEnvironmentOptions()).FirstOrDefault();
            }catch(Exception e)
            {
                Logger.Debug("Exception : " + projectAnalyzer.ProjectFile.Path);
                Logger.Debug(e.StackTrace);
                // TODO Handle errors
                // Ignore errors from vbproj until a fix from Buildalyzer
                if (!projectAnalyzer.ProjectFile.Path.EndsWith("vbproj"))
                {
                    throw;
                }
            }

            return null;
        }

        /*
         *  Set Target Framework version along with Framework settings
         *  This is to handle build issues with framework version and framework mentioned here:
         *  https://github.com/Microsoft/msbuild/issues/1805
         *  
         */
        private void setTargetFrameworkSettings(IProjectAnalyzer projectAnalyzer)
        {
            string frameworkId = projectAnalyzer.ProjectFile.TargetFrameworks.FirstOrDefault();
            if (null == frameworkId)
            {
                Logger.Debug("Target Framework not found!. Setting to default (net451)");
                frameworkId = "net451";
            }

            AnalyzerManager analyzerManager = projectAnalyzer.Manager;

            analyzerManager.RemoveGlobalProperty(TargetFramework);
            analyzerManager.RemoveGlobalProperty(TargetFrameworkVersion);
            projectAnalyzer.RemoveGlobalProperty(TargetFramework);
            projectAnalyzer.RemoveGlobalProperty(TargetFrameworkVersion);

            analyzerManager.SetGlobalProperty(TargetFramework, frameworkId);
            projectAnalyzer.SetGlobalProperty(TargetFramework, frameworkId);

            if (projectAnalyzer.ProjectFile.RequiresNetFramework)
            {
                string frameworkVerison = getTargetFrameworkVersion(frameworkId);
                analyzerManager.SetGlobalProperty(TargetFrameworkVersion, frameworkVerison);
                projectAnalyzer.SetGlobalProperty(TargetFrameworkVersion, frameworkVerison);
            }
        }

        private string getTargetFrameworkVersion(string framework)
        {
            var numericPart = Regex.Match(framework, "\\d+").Value;
            return "v" + String.Join(".", numericPart.ToCharArray());
        }

        /*
         * MSBuild properties:
         * https://docs.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-properties?view=vs-2019
         */
        private void setBuildProperties(AnalyzerManager analyzerManager)
        {
            analyzerManager.SetGlobalProperty(MsBuildProperties.DesignTimeBuild, bool.FalseString);
            analyzerManager.SetGlobalProperty(MsBuildProperties.AddModules, bool.TrueString);
            analyzerManager.SetGlobalProperty(MsBuildProperties.SkipCopyBuildProduct, bool.FalseString);
            analyzerManager.SetGlobalProperty(MsBuildProperties.CopyOutputSymbolsToOutputDirectory, bool.TrueString);
            analyzerManager.SetGlobalProperty(MsBuildProperties.CopyBuildOutputToOutputDirectory, bool.TrueString);
            analyzerManager.SetGlobalProperty(MsBuildProperties.GeneratePackageOnBuild, bool.TrueString);
            analyzerManager.SetGlobalProperty(MsBuildProperties.SkipCompilerExecution, bool.FalseString);
            analyzerManager.SetGlobalProperty(MsBuildProperties.AutoGenerateBindingRedirects, bool.TrueString);
            analyzerManager.SetGlobalProperty(MsBuildProperties.UseCommonOutputDirectory, bool.TrueString);
            analyzerManager.SetGlobalProperty(Configuration, "Release");
        }

        private EnvironmentOptions GetEnvironmentOptions()
        {
            var os = DetermineOSPlatform();
            EnvironmentOptions options = new EnvironmentOptions();

            if(os == OSPlatform.Linux || os == OSPlatform.OSX)
            {
                options.EnvironmentVariables.Add(EnvironmentVariables.MSBUILD_EXE_PATH, Constants.MsBuildCommandName);
            }            

            options.Arguments.Add(Constants.RestorePackagesConfigArgument);
            return options;
        }

        private OSPlatform DetermineOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSPlatform.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSPlatform.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
            {
                return OSPlatform.FreeBSD;
            }
            return OSPlatform.Create("None");
        }
    }
}