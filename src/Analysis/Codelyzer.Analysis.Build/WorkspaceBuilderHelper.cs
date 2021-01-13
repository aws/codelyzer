using Buildalyzer;
using Buildalyzer.Environment;
using Buildalyzer.Workspaces;
using Codelyzer.Analysis.Common;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Codelyzer.Analysis.Build
{
    public class WorkspaceBuilderHelper : IDisposable
    {
        private const string TargetFramework = nameof(TargetFramework);
        private const string TargetFrameworkVersion = nameof(TargetFrameworkVersion);
        private const string Configuration = nameof(Configuration);
        private readonly AnalyzerConfiguration _analyzerConfiguration;

        internal List<ProjectAnalysisResult> Projects;

        private ILogger Logger { get; set; }

        public WorkspaceBuilderHelper(ILogger logger, string workspacePath, AnalyzerConfiguration analyzerConfiguration = null)
        {
            this.Logger = logger;
            this.WorkspacePath = workspacePath;
            this.Projects = new List<ProjectAnalysisResult>();
            _analyzerConfiguration = analyzerConfiguration;
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
                Logger.LogInformation("Loading the Workspace (Solution): " + WorkspacePath);

                AnalyzerManager analyzerManager = new AnalyzerManager(WorkspacePath,
                   new AnalyzerManagerOptions
                   {
                       LogWriter = writer
                   });

                Logger.LogInformation("Loading the Solution Done: " + WorkspacePath);

                // AnalyzerManager builds the projects based on their dependencies
                // After this, code does not depend on Buildalyzer                
                BuildSolution(analyzerManager);
            }
            else
            {
                AnalyzerManager analyzerManager = new AnalyzerManager(new AnalyzerManagerOptions
                {
                    LogWriter = writer
                });

                var dict = new Dictionary<Guid, IAnalyzerResult>();
                using (AdhocWorkspace workspace = new AdhocWorkspace())
                {
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
                        Logger.LogInformation("Building: " + path);

                        IProjectAnalyzer projectAnalyzer = analyzerManager.GetProject(path);
                        IAnalyzerResults analyzerResults = projectAnalyzer.Build(GetEnvironmentOptions(projectAnalyzer.ProjectFile.RequiresNetFramework));
                        IAnalyzerResult analyzerResult = analyzerResults.First();


                        dict[analyzerResult.ProjectGuid] = analyzerResult;
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

                    foreach (var project in workspace.CurrentSolution.Projects)
                    {
                        try
                        {
                            var result = dict[project.Id.Id];

                            var projectAnalyzer = analyzerManager.Projects.Values.FirstOrDefault(p =>
                                p.ProjectGuid.Equals(project.Id.Id));

                            Projects.Add(new ProjectAnalysisResult()
                            {
                                Project = project,
                                AnalyzerResult = result,
                                ProjectAnalyzer = projectAnalyzer
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.LogDebug(ex.StackTrace);
                        }
                    }
                }
            }

            Logger.LogDebug(sb.ToString());
            writer.Close();
        }

        /*
         *   Build all the projects in workspace 
         *   TODO: Need to handle different type of projects like VB, CSharp, etc.,
         *   TODO: Fix needed from Buildalyzer: https://github.com/daveaglick/Buildalyzer/issues/113
         * */
        private void BuildSolution(IAnalyzerManager manager)
        {
            var options = new ParallelOptions() { MaxDegreeOfParallelism = _analyzerConfiguration.ConcurrentThreads };

            BlockingCollection<IAnalyzerResult> concurrentResults = new BlockingCollection<IAnalyzerResult>();
            Parallel.ForEach(manager.Projects.Values, options, p =>
            {
                Logger.LogDebug("Building the project : " + p.ProjectFile.Path);
                var buildResult = BuildProject(p);
                if (buildResult != null)
                {
                    concurrentResults.Add(buildResult);
                    Logger.LogDebug("Building complete for {0} - {1}", p.ProjectFile.Path, buildResult.Succeeded ? "Success" : "Fail");
                }
                else
                {
                    Logger.LogDebug("Building complete for {0} - {1}", p.ProjectFile.Path, "Fail");
                }
            });

            List<IAnalyzerResult> results = concurrentResults.ToList();

            var dict = new Dictionary<Guid, IAnalyzerResult>();
            // Add each result to a new workspace
            using (AdhocWorkspace workspace = new AdhocWorkspace())
            {
                foreach (var result in results)
                {
                    try
                    {
                        result.AddToWorkspace(workspace);
                        dict[result.ProjectGuid] = result;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug("Exception : " + result.ProjectFilePath);
                        Logger.LogDebug(ex.StackTrace);
                    }
                }

                foreach (var project in workspace.CurrentSolution.Projects)
                {
                    try
                    {
                        var result = dict[project.Id.Id];

                        var projectAnalyzer = manager.Projects.Values.FirstOrDefault(p =>
                            p.ProjectGuid.Equals(project.Id.Id));

                        Projects.Add(new ProjectAnalysisResult()
                        {
                            Project = project,
                            AnalyzerResult = result,
                            ProjectAnalyzer = projectAnalyzer
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex.StackTrace);
                    }
                }
            }
        }

        private IAnalyzerResult BuildProject(IProjectAnalyzer projectAnalyzer)
        {
            try
            {
                return projectAnalyzer.Build(GetEnvironmentOptions(projectAnalyzer.ProjectFile.RequiresNetFramework)).FirstOrDefault();
            }
            catch (Exception e)
            {
                Logger.LogDebug("Exception : " + projectAnalyzer.ProjectFile.Path);
                Logger.LogDebug(e.StackTrace);
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
         
        private void setTargetFrameworkSettings(IProjectAnalyzer projectAnalyzer)
        {
            string frameworkId = projectAnalyzer.ProjectFile.TargetFrameworks.FirstOrDefault();
            if (null == frameworkId)
            {
                Logger.LogDebug("Target Framework not found!. Setting to default (net451)");
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
        */
        /*
         * MSBuild properties:
         * https://docs.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-properties?view=vs-2019

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
        */
        private EnvironmentOptions GetEnvironmentOptions(bool requiresNetFramework)
        {
            var os = DetermineOSPlatform();
            EnvironmentOptions options = new EnvironmentOptions();

            if (os == OSPlatform.Linux || os == OSPlatform.OSX)
            {
                if (requiresNetFramework)
                {
                    options.EnvironmentVariables.Add(EnvironmentVariables.MSBUILD_EXE_PATH, Constants.MsBuildCommandName);

                }
            }

            options.EnvironmentVariables.Add(Constants.EnableNuGetPackageRestore, Boolean.TrueString.ToLower());

            options.Arguments.Add(Constants.RestorePackagesConfigArgument);
            options.Arguments.Add(Constants.LanguageVersionArgument);

            if (_analyzerConfiguration.MetaDataSettings.GenerateBinFiles)
            {
                options.GlobalProperties.Add(MsBuildProperties.CopyBuildOutputToOutputDirectory, "true");
                options.GlobalProperties.Add(MsBuildProperties.CopyOutputSymbolsToOutputDirectory, "true");
                options.GlobalProperties.Add(MsBuildProperties.UseCommonOutputDirectory, "false");
                options.GlobalProperties.Add(MsBuildProperties.SkipCopyBuildProduct, "false");
                options.GlobalProperties.Add(MsBuildProperties.SkipCompilerExecution, "false");
            }

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
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                return OSPlatform.FreeBSD;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSPlatform.OSX;
            }
            return OSPlatform.Create("None");
        }

        public void Dispose()
        {
            Projects?.ForEach(p => p.Dispose());
            Projects = null;
        }
    }
}
