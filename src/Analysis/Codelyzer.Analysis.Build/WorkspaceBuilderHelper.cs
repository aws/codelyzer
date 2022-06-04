using Buildalyzer;
using Buildalyzer.Construction;
using Buildalyzer.Environment;
using Buildalyzer.Workspaces;
using Codelyzer.Analysis.Common;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Codelyzer.Analysis.Build
{
    public class WorkspaceBuilderHelper : IDisposable
    {
        private const string TargetFramework = nameof(TargetFramework);
        private const string TargetFrameworkVersion = nameof(TargetFrameworkVersion);
        private const string Configuration = nameof(Configuration);
        private readonly AnalyzerConfiguration _analyzerConfiguration;
        private readonly AnalyzerManager _analyzerManager;
        private readonly AdhocWorkspace _workspaceIncremental;
        private StringBuilder _sb;
        private StringWriter _writer;
        private MSBuildDetector _msBuildDetector;

        internal List<ProjectAnalysisResult> Projects;
        internal List<ProjectAnalysisResult> FailedProjects;
        private Dictionary<Guid, IAnalyzerResult> DictAnalysisResult;
        private ILogger Logger { get; set; }

        public WorkspaceBuilderHelper(ILogger logger, string workspacePath, AnalyzerConfiguration analyzerConfiguration = null)
        {
            this.Logger = logger;
            this.WorkspacePath = workspacePath;
            this.Projects = new List<ProjectAnalysisResult>();
            this.FailedProjects = new List<ProjectAnalysisResult>();
            this.DictAnalysisResult = new Dictionary<Guid, IAnalyzerResult>();
            _analyzerConfiguration = analyzerConfiguration;
            _workspaceIncremental = new AdhocWorkspace();
            _sb = new StringBuilder();
            _writer = new StringWriter(_sb);
            _analyzerManager = GetAnalyzerManager();
            _msBuildDetector = new MSBuildDetector();
        }

        private string WorkspacePath { get; }

        private bool IsSolutionFile()
        {
            return WorkspacePath.EndsWith("sln");
        }

        private bool IsProjectFile(ProjectInSolution project)
        {
            return Constants.AcceptedProjectTypes.Contains(project.ProjectType);
        }

        public async IAsyncEnumerable<ProjectAnalysisResult> BuildProjectIncremental()
        {
            if (IsSolutionFile())
            {
                using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(1))
                {
                    string solutionFilePath = NormalizePath(WorkspacePath);
                    SolutionFile solutionFile = SolutionFile.Parse(solutionFilePath);
                    foreach (var project in solutionFile.ProjectsInOrder)
                    {
                        string projectPath = project.AbsolutePath;
                        if (IsProjectFile(project))
                        {
                            // if it is part of analyzer manager
                            concurrencySemaphore.Wait();
                            var result = await Task.Run(() =>
                            {
                                try
                                {
                                    return RunTask(projectPath);
                                }
                                finally
                                {
                                    concurrencySemaphore.Release();
                                }
                            });
                            yield return result;
                        }                      
                    }
                }
            }
            else
            {
                yield return BuildIncremental(WorkspacePath);
            }

            Logger.LogDebug(_sb.ToString());
            _writer.Flush();
            _writer.Close();
             ProcessLog(_writer.ToString());
        }

        private ProjectAnalysisResult RunTask(string projectPath)
        {
            Logger.LogDebug("Building the project : " + projectPath);
            var project = _workspaceIncremental.CurrentSolution?.Projects.FirstOrDefault(x => x.FilePath == projectPath);

            if (project != null)
            {
                Guid projectGuid = project.Id.Id;

                if (DictAnalysisResult.ContainsKey(projectGuid))
                {
                    IProjectAnalyzer projectAnalyzerResult = _analyzerManager.Projects.Values.FirstOrDefault(p => p.ProjectGuid.Equals(projectGuid));
                    return new ProjectAnalysisResult()
                    {
                        Project = project,
                        AnalyzerResult = DictAnalysisResult[projectGuid],
                        ProjectAnalyzer = projectAnalyzerResult
                    };
                }
            }

            return BuildIncremental(projectPath);
        }

        private ProjectAnalysisResult BuildIncremental(string projectPath)
        {
            Queue<string> queue = new Queue<string>();
            ISet<string> existing = new HashSet<string>();

            queue.Enqueue(projectPath);
            existing.Add(projectPath);

            /*
             * We need to resolve all the project dependencies to avoid compilation errors.
             * If we have compilation errors, we might miss some of the semantic values.
             */
            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                Logger.LogInformation("Building: " + path);

                IProjectAnalyzer projectAnalyzer = _analyzerManager.GetProject(path);

                if (!TryGetRequiresNetFramework(projectAnalyzer.ProjectFile, out bool requiresNetFramework))
                {
                    continue;
                }

                if (_analyzerConfiguration.BuildSettings.BuildOnly)
                {
                    BuildSolutionOnlyWithoutOutput(WorkspacePath, requiresNetFramework);

                    return null;
                }

                IAnalyzerResult analyzerResult = projectAnalyzer.Build(GetEnvironmentOptions(requiresNetFramework, projectAnalyzer.ProjectFile.ToolsVersion)).FirstOrDefault();

                if (analyzerResult == null)
                {
                    Logger.LogDebug("Building complete for {0} - {1}", path, "Fail");
                    return new ProjectAnalysisResult()
                    {
                        ProjectAnalyzer = projectAnalyzer
                    };
                }

                if(!DictAnalysisResult.ContainsKey(analyzerResult.ProjectGuid))
                {
                    DictAnalysisResult[analyzerResult.ProjectGuid] = analyzerResult;
                    analyzerResult.AddToWorkspace(_workspaceIncremental);

                    foreach (var pref in analyzerResult.ProjectReferences)
                    {
                        if (!existing.Contains(pref))
                        {
                            existing.Add(pref);
                            queue.Enqueue(pref);
                        }
                    }                   
                }
            }
            
            Project project = _workspaceIncremental.CurrentSolution?.Projects.FirstOrDefault(x => x.FilePath.Equals(projectPath));
            Logger.LogDebug("Building complete for {0} - {1}", projectPath, DictAnalysisResult[project.Id.Id].Succeeded ? "Success" : "Fail");
            return new ProjectAnalysisResult()
            {
                Project = project,
                AnalyzerResult = DictAnalysisResult[project.Id.Id],
                ProjectAnalyzer = _analyzerManager.Projects.Values.FirstOrDefault(p => p.ProjectGuid.Equals(project.Id.Id))
            };
        }


        public void Build()
        {
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
                       LogWriter = _writer
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
                    LogWriter = _writer
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
                        
                        if (!TryGetRequiresNetFramework(projectAnalyzer.ProjectFile, out bool requiresNetFramework))
                        {
                            continue;
                        }

                        IAnalyzerResults analyzerResults = projectAnalyzer.Build(GetEnvironmentOptions(requiresNetFramework, projectAnalyzer.ProjectFile.ToolsVersion));
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

            Logger.LogDebug(_sb.ToString());
            _writer.Flush();
            _writer.Close();
            ProcessLog(_writer.ToString());
        }


        public void GenerateNoBuildAnalysis()
        {
            if (IsSolutionFile())
            {
                Logger.LogInformation("Loading the Workspace (Solution): " + WorkspacePath);

                AnalyzerManager analyzerManager = new AnalyzerManager(WorkspacePath,
                   new AnalyzerManagerOptions
                   {
                       LogWriter = _writer
                   });

                analyzerManager.Projects.Values.ToList().ForEach(projectAnalyzer =>
                {
                    Projects.Add(new ProjectAnalysisResult()
                    {
                        ProjectAnalyzer = projectAnalyzer
                    });
                });

                Logger.LogInformation("Loading the Solution Done: " + WorkspacePath);
            }
            else
            {
                AnalyzerManager analyzerManager = new AnalyzerManager(new AnalyzerManagerOptions
                {
                    LogWriter = _writer
                });

                IProjectAnalyzer projectAnalyzer = analyzerManager.GetProject(WorkspacePath);
                Projects.Add(new ProjectAnalysisResult()
                {
                    ProjectAnalyzer = projectAnalyzer
                });
            }

            Logger.LogDebug(_sb.ToString());
            _writer.Flush();
            _writer.Close();
            ProcessLog(_writer.ToString());
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
            var isSupportedProject = TryGetRequiresNetFramework(manager.Projects.First().Value.ProjectFile, out var isFramework);            

            //If we are building only, we don't need to run through the rest of the logic
            if (_analyzerConfiguration.BuildSettings.BuildOnly)
            {
                if (isSupportedProject)
                {
                    BuildSolutionOnlyWithoutOutput(WorkspacePath, isFramework);
                }
                return;
            }
            var options = new ParallelOptions() { MaxDegreeOfParallelism = _analyzerConfiguration.ConcurrentThreads };

            // If core solution, building multiple projects at the same time causes conflicts when there are dependencies between the projects:
            if (!isFramework)
            {
                options.MaxDegreeOfParallelism = 1;
            }
            BlockingCollection<IAnalyzerResult> concurrentResults = new BlockingCollection<IAnalyzerResult>();
            Parallel.ForEach(manager.Projects.Values, options, p =>
            {
                Logger.LogDebug("Building the project : " + p.ProjectFile.Path);
                
                if(IsProjectFile(p.ProjectInSolution))
                {
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
                }
                else
                {
                    Logger.LogDebug("Building skipped for {0} - {1}", p.ProjectFile.Path, "Skipped");
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

        private void BuildSolutionOnlyWithoutOutput(string solutionPath, bool isFramework)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                var msBuildLocation = _analyzerConfiguration.BuildSettings.MSBuildPath;
                if (string.IsNullOrEmpty(msBuildLocation))
                {
                    msBuildLocation = _msBuildDetector.GetFirstMatchingMsBuildFromPath();
                }

                //Add quotes around solution name to make sure spaces don't cause the command to fail
                var escapedSolutionPath = "\"" + solutionPath + "\"";

                var arguments = new List<string>(_analyzerConfiguration.BuildSettings.BuildArguments);

                if (isFramework)
                {
                    startInfo.FileName = msBuildLocation;
                    arguments.Insert(0, escapedSolutionPath);
                    startInfo.Arguments = string.Join(" ", arguments);
                }
                else
                {
                    startInfo.FileName = "dotnet";
                    arguments.Insert(0, escapedSolutionPath);
                    arguments.Insert(0, "build");
                    startInfo.Arguments = string.Join(" ", arguments);
                }

                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, "Unable to build. Project type used is not supported");
                throw;
            }
        }

        private IAnalyzerResult BuildProject(IProjectAnalyzer projectAnalyzer)
        {
            try
            {
                if (!TryGetRequiresNetFramework(projectAnalyzer.ProjectFile, out bool requiresNetFramework))
                {
                    return null;
                }
                return projectAnalyzer.Build(GetEnvironmentOptions(requiresNetFramework, projectAnalyzer.ProjectFile.ToolsVersion)).FirstOrDefault();
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

        private bool TryGetRequiresNetFramework(IProjectFile projectFile, out bool requiresNetFramework)
        {
            requiresNetFramework = false;
            /*
                We need to have this property in a try/catch because there are cases when there are additional Import or LanguageTarget tags
                with unexpected (or missing) attributes. This avoids a NPE in buildalyzer code retrieving this property                  
                */
            try
            {
                requiresNetFramework = projectFile.RequiresNetFramework;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while checking if project is a framework project. Buildalyzer does not support this type of project");
                return false;
            }
            return true;
        }

        private EnvironmentOptions GetEnvironmentOptions(bool requiresNetFramework, string toolsVersion)
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

            //We want to provide the MsBuild path only if it's a framework solution. Buildalyzer automatically builds core solutions using "dotnet"
            if (requiresNetFramework)
            {
                try
                {
                    var msbuildExe = _analyzerConfiguration.BuildSettings.MSBuildPath;
                    if (string.IsNullOrEmpty(msbuildExe))
                    {
                        msbuildExe = _msBuildDetector.GetFirstMatchingMsBuildFromPath(toolsVersion: toolsVersion);
                    }
                    if (!string.IsNullOrEmpty(msbuildExe)) options.EnvironmentVariables.Add(EnvironmentVariables.MSBUILD_EXE_PATH, msbuildExe);
                    else { throw new Exception(); }

                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Build error: Codelyzer wasn't able to retrieve the MSBuild path");
                }
                _analyzerConfiguration.BuildSettings.BuildArguments.ForEach(argument => {
                    options.Arguments.Add(argument);
                });
            }
            options.EnvironmentVariables.Add(Constants.EnableNuGetPackageRestore, Boolean.TrueString.ToLower());

            if (_analyzerConfiguration.MetaDataSettings.GenerateBinFiles)
            {
                options.GlobalProperties.Add(MsBuildProperties.CopyBuildOutputToOutputDirectory, "true");
                options.GlobalProperties.Add(MsBuildProperties.CopyOutputSymbolsToOutputDirectory, "true");
                options.GlobalProperties.Add(MsBuildProperties.UseCommonOutputDirectory, "false");
                options.GlobalProperties.Add(MsBuildProperties.SkipCopyBuildProduct, "false");
                options.GlobalProperties.Add(MsBuildProperties.SkipCompilerExecution, "false");
                if (!requiresNetFramework)
                {
                    options.GlobalProperties.Add(MsBuildProperties.BuildProjectReferences, "true");
                }
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

        private AnalyzerManager GetAnalyzerManager()
        {
            AnalyzerManager analyzerManager;
            var analyzerManagerOptions = new AnalyzerManagerOptions
            {
                LogWriter = _writer
            };

            if (IsSolutionFile())
            {
                
                analyzerManager = new AnalyzerManager(WorkspacePath, analyzerManagerOptions);
            }
            else
            {
                analyzerManager = new AnalyzerManager(analyzerManagerOptions);

            }
            return analyzerManager;
        }

        public void Dispose()
        {
            Projects?.ForEach(p => p.Dispose());
            Projects = null;
        }

        private string NormalizePath(string path) =>
            path == null ? null : Path.GetFullPath(path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));


    }
}