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
using System.Diagnostics;
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
        }

        private string WorkspacePath { get; }

        private bool IsSolutionFile()
        {
            return WorkspacePath.EndsWith("sln");
        }

        private bool IsProjectFile(string projectPath)
        {
            return projectPath.EndsWith("csproj");
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
                        if (IsProjectFile(projectPath))
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

        private ProjectAnalysisResult BuildIncremental(string WorkspacePath)
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

                IProjectAnalyzer projectAnalyzer = _analyzerManager.GetProject(path);

                if (!TryGetRequiresNetFramework(projectAnalyzer.ProjectFile, out bool requiresNetFramework))
                {
                    continue;
                }
                IAnalyzerResult analyzerResult = projectAnalyzer.Build(GetEnvironmentOptions(requiresNetFramework)).FirstOrDefault();

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
                    projectAnalyzer.AddToWorkspace(_workspaceIncremental);

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
            
            Project project = _workspaceIncremental.CurrentSolution?.Projects.FirstOrDefault(x => x.FilePath.Equals(WorkspacePath));
            Logger.LogDebug("Building complete for {0} - {1}", WorkspacePath, DictAnalysisResult[project.Id.Id].Succeeded ? "Success" : "Fail");
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

                        IAnalyzerResults analyzerResults = projectAnalyzer.Build(GetEnvironmentOptions(requiresNetFramework));
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
                if (!TryGetRequiresNetFramework(projectAnalyzer.ProjectFile, out bool requiresNetFramework))
                {
                    return null;
                }
                return projectAnalyzer.Build(GetEnvironmentOptions(requiresNetFramework)).FirstOrDefault();
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

            try
            {
                var msbuildExe = GetFrameworkMsBuildExePath();
                if (!String.IsNullOrEmpty(msbuildExe)) options.EnvironmentVariables.Add(EnvironmentVariables.MSBUILD_EXE_PATH, msbuildExe);
                else { throw new Exception(); }
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, "Build error: Codelyzer wasn't able to retrieve the MSBuild path");
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

        public static string GetFrameworkMsBuildExePath(string programFilesPath = null, string programFilesX86Path = null)
        {
            // Could not find the tools path, possibly due to https://github.com/Microsoft/msbuild/issues/2369
            // Try to poll for it. From https://github.com/KirillOsenkov/MSBuildStructuredLog/blob/4649f55f900a324421bad5a714a2584926a02138/src/StructuredLogViewer/MSBuildLocator.cs

            List<string> editions = new List<string> { "Enterprise", "Professional", "Community", "BuildTools" };
            var targets = new string[] { "Microsoft.CSharp.targets", "Microsoft.CSharp.CurrentVersion.targets", "Microsoft.Common.targets" };
            // "Microsoft.CSharp.CrossTargeting.targets"

            var msbuildpath = "";
            DirectoryInfo vsDirectory;
            var programFiles = programFilesPath ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = programFilesX86Path ?? System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86);

           
            //2022              
            vsDirectory = new DirectoryInfo(Path.Combine(programFiles, "Microsoft Visual Studio"));
            msbuildpath = GetMsBuildPathFromVSDirectory(vsDirectory, editions, targets);
            if (!string.IsNullOrEmpty(msbuildpath))
            {
                return msbuildpath;
            }

            // 2019, 2017
            vsDirectory = new DirectoryInfo(Path.Combine(programFilesX86, "Microsoft Visual Studio"));
            msbuildpath = GetMsBuildPathFromVSDirectory(vsDirectory, editions, targets);
            if (!string.IsNullOrEmpty(msbuildpath))
            {
                return msbuildpath;
            }
            
            // 14.0, 12.0 
            vsDirectory = new DirectoryInfo(Path.Combine(programFilesX86, "MSBuild"));
            msbuildpath = GetMsBuildPathFromVSDirectoryBelow15(vsDirectory, editions, targets);
            if (!string.IsNullOrEmpty(msbuildpath))
            {
                return msbuildpath;
            }

            return msbuildpath;
        }

        public static string GetMsBuildPathFromVSDirectory(DirectoryInfo vsDirectory, List<string> editions, string[] targets)
        {
            if (vsDirectory.Exists)
            {   List<FileInfo> msBuildExePath = vsDirectory
                    .GetDirectories("MSBuild", SearchOption.AllDirectories)
                    .SelectMany(msBuildDir => msBuildDir.GetFiles("MSBuild.exe", SearchOption.AllDirectories))
                    .OrderByDescending(msbuild => FileVersionInfo.GetVersionInfo(msbuild.FullName).FileVersion)
                    .ThenBy(msbuild => editions.IndexOf(GetEditionType(msbuild.DirectoryName, editions)))
                    .ThenBy(msbuild =>
                    {
                        var folderName = GetVersionFolder(msbuild.FullName);
                        if (folderName.ToLower() == "current") return -100;
                        else
                        {
                            if(double.TryParse(folderName, out double result))
                            {
                                return -1 * result;
                            }
                            return -100;
                        }
                    })
                    .Where(msbuild =>
                    {
                        var targetsWithPath = GetTargetsWithPath(msbuild.DirectoryName, targets);
                        if (targetsWithPath.TrueForAll(File.Exists)) return true;
                        return false;
                    })
                    .ToList();
                return msBuildExePath?.FirstOrDefault()?.FullName;
            };
            return "";
        }

        public static string GetMsBuildPathFromVSDirectoryBelow15(DirectoryInfo vsDirectory, List<string> editions, string[] targets)
        {
            if (vsDirectory.Exists)
            {
                List<FileInfo> msBuildExePath = vsDirectory
                    .GetFiles("MSBuild.exe", SearchOption.AllDirectories)
                    .OrderByDescending(msbuild => FileVersionInfo.GetVersionInfo(msbuild.FullName).FileVersion)
                    .ThenBy(msbuild =>
                    {
                        var folderName = GetVersionFolder(msbuild.FullName);
                        if (folderName.ToLower() == "current") return 1;
                        else return -Convert.ToDouble(folderName);

                    })
                    .Where(msbuild =>
                    {
                        var targetsWithPath = GetTargetsWithPath(msbuild.DirectoryName, targets);
                        if (targetsWithPath.TrueForAll(File.Exists)) return true;
                        return false;
                    })
                    .ToList();
                return msBuildExePath?.First().FullName;
            };
            return "";
        }

        public static string GetEditionType(string vsPath, List<string> editions)
        {
            string[] elements = vsPath.Split(Path.DirectorySeparatorChar);
            foreach (var edition in editions)
            {
                if (elements.Contains(edition))
                {
                    return edition;
                }
            }
            return "";
        }

        public static string GetVersionFolder(string vsPath)
        {
            List<string> elements = vsPath.Split(Path.DirectorySeparatorChar).ToList();
            var folderIdx = elements.IndexOf("MSBuild");
            return elements[folderIdx + 1];
        }
        public static List<string> GetTargetsWithPath(string vsPath, string[] targets)
        {
            List<string> targetsWithPath = new List<string>();
            foreach (string target in targets)
            {
                targetsWithPath.Add(Path.Combine(vsPath, target));
            }
            return targetsWithPath;
        }
    }
}