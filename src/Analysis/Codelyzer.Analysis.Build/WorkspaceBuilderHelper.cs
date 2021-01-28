using Buildalyzer;
using Buildalyzer.Construction;
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
        internal List<ProjectAnalysisResult> FailedProjects;

        private ILogger Logger { get; set; }

        public WorkspaceBuilderHelper(ILogger logger, string workspacePath, AnalyzerConfiguration analyzerConfiguration = null)
        {
            this.Logger = logger;
            this.WorkspacePath = workspacePath;
            this.Projects = new List<ProjectAnalysisResult>();
            this.FailedProjects = new List<ProjectAnalysisResult>();
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
                        IAnalyzerResults analyzerResults = projectAnalyzer.Build(GetEnvironmentOptions(projectAnalyzer.ProjectFile));
                        IAnalyzerResult analyzerResult = analyzerResults.First();

                        if (analyzerResult == null)
                        {
                            FailedProjects.Add(new ProjectAnalysisResult()
                            {
                                ProjectAnalyzer = projectAnalyzer
                            });
                        }

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
            writer.Flush();
            writer.Close();
            ProcessLog(writer.ToString());
        }

        private void ProcessLog(string currentLog)
        {
            if (currentLog.Contains(KnownErrors.MsBuildMissing))
            {
                Logger.LogError("Build error: Missing MSBuild Path");
            }
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
                    FailedProjects.Add(new ProjectAnalysisResult()
                    {
                        ProjectAnalyzer = p
                    });
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
                return projectAnalyzer.Build(GetEnvironmentOptions(projectAnalyzer.ProjectFile)).FirstOrDefault();
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

        private EnvironmentOptions GetEnvironmentOptions(IProjectFile projectFile)
        {
            var os = DetermineOSPlatform();
            EnvironmentOptions options = new EnvironmentOptions();

            if (os == OSPlatform.Linux || os == OSPlatform.OSX)
            {
                var requiresNetFramework = false;
                /*
                    We need to have this property in a try/catch because there are cases when there are additional Import or LanguageTarget tags
                    with unexpected (or missing) attributes. This avoids a NPE in buildalyzer code retrieving this property                  
                 */
                try
                {
                    requiresNetFramework = projectFile.RequiresNetFramework;
                }
                catch(Exception ex)
                {
                    Logger.LogError(ex, "Error while checking if project is a framework project");
                }
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
