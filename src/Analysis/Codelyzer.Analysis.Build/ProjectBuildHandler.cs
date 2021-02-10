using Codelyzer.Analysis.Model;
using Buildalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Constants = Codelyzer.Analysis.Common.Constants;
using System.Xml.Linq;
using System.Xml;

namespace Codelyzer.Analysis.Build
{    
    public class ProjectBuildHandler : IDisposable
    {
        private Project Project;
        private Compilation Compilation;
        private List<string> Errors { get; set; }
        private ILogger Logger;
        private readonly AnalyzerConfiguration _analyzerConfiguration;
        internal IAnalyzerResult AnalyzerResult;
        internal IProjectAnalyzer ProjectAnalyzer;
        internal bool isSyntaxAnalysis;

        private const string syntaxAnalysisError = "Build Errors: Encountered an unknown build issue. Falling back to syntax analysis";


        private async Task SetCompilation()
        {
            Compilation = await Project.GetCompilationAsync();
            var errors = Compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            if (errors.Any())
            {
                Logger.LogError($"Build Errors: {Compilation.AssemblyName}: {errors.Count()} " +
                                $"compilation errors: \n\t{string.Join("\n\t", errors.Where(e => false).Select(e => e.ToString()))}");
                Logger.LogDebug(String.Join("\n", errors));

                foreach (var error in errors)
                {
                    Errors.Add(error.ToString());
                }
            }
            else
            {
                Logger.LogInformation($"Project {Project.Name} compiled with no errors");
            }
            
            // Fallback logic: On fatal errors like msbuild is not installed or framework versions not installed
            // the build fails and does not give syntax trees. 
            if (Compilation.SyntaxTrees == null ||
                Compilation.SyntaxTrees.Count() == 0)
            {
                try
                {
                    Logger.LogError(syntaxAnalysisError);                   
                    Errors.Add(syntaxAnalysisError);

                    FallbackCompilation();
                    isSyntaxAnalysis = true;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error while running syntax analysis");
                    Console.WriteLine(e);
                }
            }
        }

        private void FallbackCompilation()
        {
            var options = (CSharpCompilationOptions) this.Project.CompilationOptions;
            var meta = this.Project.MetadataReferences;
            var trees = new List<SyntaxTree>();

            var projPath = Path.GetDirectoryName(Project.FilePath);
            DirectoryInfo directory = new DirectoryInfo(projPath);
            var allFiles = directory.GetFiles("*.cs", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                try
                {
                    using (var stream = File.OpenRead(file.FullName))
                    {
                        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(stream), path: file.FullName);
                        trees.Add(syntaxTree);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            
            if (trees.Count != 0)
            {
                Compilation = CSharpCompilation.Create(Project.AssemblyName, trees, meta, options);
            }
        }
        private void SetSyntaxCompilation()
        {
            var trees = new List<SyntaxTree>();
            isSyntaxAnalysis = true;

            Logger.LogError(syntaxAnalysisError);
            Errors.Add(syntaxAnalysisError);

            var projPath = Path.GetDirectoryName(ProjectAnalyzer.ProjectFile.Path);
            DirectoryInfo directory = new DirectoryInfo(projPath);
            var allFiles = directory.GetFiles("*.cs", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                try
                {
                    using (var stream = File.OpenRead(file.FullName))
                    {
                        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(stream), path: file.FullName);
                        trees.Add(syntaxTree);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error while running syntax analysis");
                    Console.WriteLine(e);
                }
            }

            if (trees.Count != 0)
            {
                Compilation = CSharpCompilation.Create(ProjectAnalyzer.ProjectInSolution.ProjectName, trees);
            }
        }
        private static void DisplayProjectProperties(Project project)
        {
            Console.WriteLine($" Project: {project.Name}");
            Console.WriteLine($" Assembly name: {project.AssemblyName}");
            Console.WriteLine($" Language: {project.Language}");
            Console.WriteLine($" Project file: {project.FilePath}");
            Console.WriteLine($" Output file: {project.OutputFilePath}");
            Console.WriteLine($" Documents: {project.Documents.Count()}");
            Console.WriteLine($" Metadata references: {project.MetadataReferences.Count}");
            Console.WriteLine($" Metadata references: {String.Join("\n", project.MetadataReferences)}");
            Console.WriteLine($" Project references: {project.ProjectReferences.Count()}");
            Console.WriteLine($" Project references: {String.Join("\n", project.ProjectReferences)}");
            Console.WriteLine();
        }
        public ProjectBuildHandler(ILogger logger, AnalyzerConfiguration analyzerConfiguration = null)
        {
            Logger = logger;
            _analyzerConfiguration = analyzerConfiguration;

            Errors = new List<string>();
        }
        public ProjectBuildHandler(ILogger logger, Project project, AnalyzerConfiguration analyzerConfiguration = null)
        {
            Logger = logger;
            _analyzerConfiguration = analyzerConfiguration;

            CompilationOptions options = project.CompilationOptions;

            if (project.CompilationOptions is CSharpCompilationOptions)
            {
                /*
                 * This is to fix the compilation errors related to :
                 * Compile errors for assemblies which reference to mscorlib 2.0.5.0 (LINQPad 5.00.08)
                 * https://forum.linqpad.net/discussion/856/compile-errors-for-assemblies-which-reference-to-mscorlib-2-0-5-0-linqpad-5-00-08
                 */
                options = options.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);
            }

            this.Project = project.WithCompilationOptions(options);
            Errors = new List<string>();
        }
        public async Task<ProjectBuildResult> Build()
        {
            await SetCompilation();
            ProjectBuildResult projectBuildResult = new ProjectBuildResult
            {
                BuildErrors = Errors,
                ProjectPath = Project.FilePath,
                ProjectRootPath = Path.GetDirectoryName(Project.FilePath),
                Project = Project,
                Compilation = Compilation,
                IsSyntaxAnalysis = isSyntaxAnalysis
            };

            GetTargetFrameworks(projectBuildResult, AnalyzerResult);
            projectBuildResult.ProjectGuid = ProjectAnalyzer.ProjectGuid.ToString();
            projectBuildResult.ProjectType = ProjectAnalyzer.ProjectInSolution != null ? ProjectAnalyzer.ProjectInSolution.ProjectType.ToString() : string.Empty;
            
            foreach (var syntaxTree in Compilation.SyntaxTrees)
            {
                var sourceFilePath = Path.GetRelativePath(projectBuildResult.ProjectRootPath, syntaxTree.FilePath);
                var fileResult = new SourceFileBuildResult
                {
                    SyntaxTree = syntaxTree,
                    SemanticModel = Compilation.GetSemanticModel(syntaxTree),
                    SourceFileFullPath = syntaxTree.FilePath,
                    SyntaxGenerator = SyntaxGenerator.GetGenerator(Project),
                    SourceFilePath = sourceFilePath
                };
                projectBuildResult.SourceFileBuildResults.Add(fileResult);
                projectBuildResult.SourceFiles.Add(sourceFilePath);
            }

            if (_analyzerConfiguration != null && _analyzerConfiguration.MetaDataSettings.ReferenceData)
            {
                projectBuildResult.ExternalReferences = GetExternalReferences(projectBuildResult);
            }

            return projectBuildResult;
        }

        public ProjectBuildResult SyntaxOnlyBuild()
        {
            SetSyntaxCompilation();

            ProjectBuildResult projectBuildResult = new ProjectBuildResult
            {
                BuildErrors = Errors,
                ProjectPath = ProjectAnalyzer.ProjectFile.Path,
                ProjectRootPath = Path.GetDirectoryName(ProjectAnalyzer.ProjectFile.Path),
                Compilation = Compilation,
                IsSyntaxAnalysis = isSyntaxAnalysis
            };

            projectBuildResult.ProjectGuid = ProjectAnalyzer.ProjectGuid.ToString();
            projectBuildResult.ProjectType = ProjectAnalyzer.ProjectInSolution != null ? ProjectAnalyzer.ProjectInSolution.ProjectType.ToString() : string.Empty;

            foreach (var syntaxTree in Compilation.SyntaxTrees)
            {
                var sourceFilePath = Path.GetRelativePath(projectBuildResult.ProjectRootPath, syntaxTree.FilePath);
                var fileResult = new SourceFileBuildResult
                {
                    SyntaxTree = syntaxTree,
                    SemanticModel = Compilation.GetSemanticModel(syntaxTree),
                    SourceFileFullPath = syntaxTree.FilePath,
                    SourceFilePath = sourceFilePath
                };
                projectBuildResult.SourceFileBuildResults.Add(fileResult);
                projectBuildResult.SourceFiles.Add(sourceFilePath);
            }

            return projectBuildResult;
        }
        private void GetTargetFrameworks(ProjectBuildResult result, Buildalyzer.IAnalyzerResult analyzerResult)
        {
            result.TargetFramework = analyzerResult.TargetFramework;
            var targetFrameworks = analyzerResult.GetProperty(Constants.TargetFrameworks);
            if (!string.IsNullOrEmpty(targetFrameworks))
            {
                result.TargetFrameworks = targetFrameworks.Split(';').ToList();
            }
        }
        private ExternalReferences GetExternalReferences(ProjectBuildResult projectResult)
        {
            ExternalReferences externalReferences = new ExternalReferences();
            if (projectResult != null && projectResult.Compilation != null)
            {
                var project = projectResult.Project;
                var projectReferencesIds = project.ProjectReferences != null ? project.ProjectReferences.Select(pr => pr.ProjectId).ToList() : null;
                var projectReferences = projectReferencesIds != null ? project.Solution.Projects.Where(p => projectReferencesIds.Contains(p.Id)) : null;
                var projectReferenceNames = projectReferences != null ? projectReferences.Select(p => p.Name).ToHashSet<string>() : null;

                externalReferences.ProjectReferences.AddRange(projectReferences.Select(p => new ExternalReference() {
                    Identity = p.Name,
                    AssemblyLocation = p.FilePath,
                    Version = p.Version.ToString()
                }));

                LoadProjectPackages(externalReferences, Directory.GetParent(project.FilePath).FullName);                

                var compilation = projectResult.Compilation;
                var externalReferencesMetaData = compilation.ExternalReferences;

                foreach (var externalReferenceMetaData in externalReferencesMetaData)
                {
                    try
                    {
                        var symbol = compilation.GetAssemblyOrModuleSymbol(externalReferenceMetaData) as IAssemblySymbol;

                        var filePath = externalReferenceMetaData.Display;
                        var name = Path.GetFileNameWithoutExtension(externalReferenceMetaData.Display);
                        var externalReference = new ExternalReference()
                        {
                            AssemblyLocation = filePath
                        };

                        if (symbol != null && symbol.Identity != null)
                        {
                            externalReference.Identity = symbol.Identity.Name;
                            externalReference.Version = symbol.Identity.Version != null ? symbol.Identity.Version.ToString() : string.Empty;
                            name = symbol.Identity.Name;
                        }

                        var nugetRef = externalReferences.NugetReferences.FirstOrDefault(n => n.Identity == name);

                        if (nugetRef == null)
                        {
                            nugetRef = externalReferences.NugetReferences.FirstOrDefault(n => filePath.ToLower().Contains(string.Concat(Constants.PackagesDirectoryIdentifier, n.Identity.ToLower(), ".", n.Version)));
                        }

                        if (nugetRef != null)
                        {
                            //Nuget with more than one dll?
                            nugetRef.AssemblyLocation = filePath;

                            //If version isn't resolved, get from external reference
                            if (string.IsNullOrEmpty(nugetRef.Version) || !Regex.IsMatch(nugetRef.Version, @"([0-9])+(\.)([0-9])+(\.)([0-9])+"))
                            {
                                nugetRef.Version = externalReference.Version;
                            }
                        }
                        else if (filePath.Contains(Common.Constants.PackagesDirectoryIdentifier, System.StringComparison.CurrentCultureIgnoreCase))
                        {
                            externalReferences.NugetDependencies.Add(externalReference);
                        }
                        else if (!projectReferenceNames.Any(n => n.StartsWith(name)))
                        {
                            externalReferences.SdkReferences.Add(externalReference);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error while resolving reference {0}", externalReferenceMetaData);
                    }
                }
            }
            return externalReferences;
        }
        private void LoadProjectPackages(ExternalReferences externalReferences , string projectDir)
        {
            //Buildalyzer was able to get the packages, use that:
            if(AnalyzerResult.PackageReferences != null && AnalyzerResult.PackageReferences.Count > 0)
            {
                externalReferences.NugetReferences.AddRange(AnalyzerResult.PackageReferences.Select(n => new ExternalReference()
                {
                    Identity = n.Key,
                    Version = n.Value.GetValueOrDefault(Constants.Version)
                }));

            //Buildalyzer wasn't able to get the packages (old format). Use packages.config to load
            } else
            {
                var nugets = LoadPackages(projectDir);
                externalReferences.NugetReferences.AddRange(nugets.Select(n => new ExternalReference() { Identity = n.PackageIdentity.Id,Version = n.PackageIdentity.Version.OriginalVersion }));
            }
        }
        private IEnumerable<PackageReference> LoadPackages(string projectDir)
        {
            IEnumerable<PackageReference> packageReferences = new List<PackageReference>();

            string packagesFile = Path.Combine(projectDir, "packages.config");

            if (File.Exists(packagesFile))
            {
                try
                {
                    XDocument xDocument = NuGet.Common.XmlUtility.Load(packagesFile);
                    var reader = new PackagesConfigReader(xDocument);
                    packageReferences = reader.GetPackages();
                }
                catch (XmlException ex)
                {
                    Logger.LogError(ex, "Error while parsing xml for file {0}", packagesFile);
                }
                catch(Exception ex)
                {
                    Logger.LogError(ex, "Error while parsing file {0}", packagesFile);
                }
            }
            return packageReferences;
        }

        public void Dispose()
        {
            Compilation = null;
            AnalyzerResult = null;
            ProjectAnalyzer = null;
            Project = null;
            Logger = null;
        }
    }
}
