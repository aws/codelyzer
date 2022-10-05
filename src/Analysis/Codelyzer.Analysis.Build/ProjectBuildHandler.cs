using Buildalyzer;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.VisualBasic;
using Constants = Codelyzer.Analysis.Common.Constants;
using LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;

namespace Codelyzer.Analysis.Build
{
    public class ProjectBuildHandler : IDisposable
    {
        private Project Project;
        private Compilation Compilation;
        private Compilation PrePortCompilation;
        private List<string> PrePortMetaReferences;
        private List<string> MissingMetaReferences { get; set; }

        private List<string> Errors { get; set; }
        private ILogger Logger;
        private readonly AnalyzerConfiguration _analyzerConfiguration;
        internal IAnalyzerResult AnalyzerResult;
        internal IProjectAnalyzer ProjectAnalyzer;
        internal bool isSyntaxAnalysis;
        private List<string> _metaReferences;
        private string _projectPath;

        private const string syntaxAnalysisError = "Build Errors: Encountered an unknown build issue. Falling back to syntax analysis";

        private XDocument LoadProjectFile(string projectFilePath)
        {
            if (!File.Exists(projectFilePath))
            {
                return null;
            }
            try
            {
                return XDocument.Load(projectFilePath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading project file {}", projectFilePath);
                return null;
            }

        }
        private List<PortableExecutableReference> LoadMetadataReferences(XDocument projectFile)
        {
            var references = new List<PortableExecutableReference>();

            if (projectFile == null) {
                return references;
            }

            var fileReferences = ExtractFileReferencesFromProject(projectFile);
            fileReferences?.ForEach(fileRef =>
            {
                if(!File.Exists(fileRef)) {
                    MissingMetaReferences.Add(fileRef);
                    Logger.LogWarning("Assembly {} referenced does not exist.", fileRef);
                    return;
                }
                try
                {
                    references.Add(MetadataReference.CreateFromFile(fileRef));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while parsing metadata reference {}.", fileRef);
                }

            });

            return references;
        }

        private List<string> ExtractFileReferencesFromProject(XDocument projectFileContents)
        {
            if (projectFileContents == null)
            {
                return null;
            }

            var portingNode = projectFileContents.Descendants()
                .FirstOrDefault(d => 
                    d.Name.LocalName == "ItemGroup"
                    && d.FirstAttribute?.Name == "Label" 
                    && d.FirstAttribute?.Value == "PortingInfo");

            var fileReferences = portingNode?.FirstNode?.ToString()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)?
                .Where(s => !(s.Contains("<!-") || s.Contains("-->")))
                .Select(s => s.Trim())
                .ToList();

            return fileReferences;
        }

        private async Task<Compilation> SetPrePortCompilation()
        {
            var preportReferences = LoadMetadataReferences(LoadProjectFile(Project.FilePath));
            if (preportReferences.Count > 0)
            {
                var preportProject = Project.WithMetadataReferences(preportReferences);
                PrePortMetaReferences = preportReferences.Select(m => m.Display).ToList();
                return await preportProject.GetCompilationAsync();
            }

            return null;
        }
        
        private bool CanSkipErrorsForVisualBasic()
        {
            // Compilation returns false build errors, it seems like we can work around this with
            // MSBuildWorkspace instead of using an AdhocWorkspace
            return Compilation != null &&
                   Compilation.Language == "Visual Basic" &&
                   AnalyzerResult.Succeeded &&
                   Compilation.SyntaxTrees.Any() &&
                   Compilation.GetSemanticModel(Compilation.SyntaxTrees.First()) != null;
        }
        private async Task SetCompilation()
        {
            PrePortCompilation = await SetPrePortCompilation();

            if (Project.Language == "Visual Basic")
            {
                var netFrameworkPath = AnalyzerResult.Properties
                    .FirstOrDefault(s => s.Key == "FrameworkPathOverride")
                    .Value;
                if (!string.IsNullOrEmpty(netFrameworkPath))
                {
                    Project = Project.AddMetadataReference(
                        MetadataReference.CreateFromFile(
                            $"{netFrameworkPath}\\mscorlib.dll"));
                }
            }
            Compilation = await Project.GetCompilationAsync();
            var errors = Compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            if (errors.Any() && !CanSkipErrorsForVisualBasic())
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
                }
            }
        }   

        private void FallbackCompilation()
        {
            var vbOptions =
                Project.CompilationOptions is VisualBasicCompilationOptions
                    ? (VisualBasicCompilationOptions) Project.CompilationOptions
                    : null;
            var options = vbOptions != null ? null : (CSharpCompilationOptions) Project.CompilationOptions;
            var meta = this.Project.MetadataReferences;
            var trees = new List<SyntaxTree>();

            var projPath = Path.GetDirectoryName(Project.FilePath);
            DirectoryInfo directory = new DirectoryInfo(projPath);

            if (vbOptions == null)
            {
                var allCSharpFiles = directory.GetFiles("*.cs", SearchOption.AllDirectories);
                foreach (var file in allCSharpFiles)
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
            }
            else 
            {
                var allVbFiles = directory.GetFiles("*.vb", SearchOption.AllDirectories);
                foreach (var file in allVbFiles)
                {
                    try
                    {

                        using (var stream = File.OpenRead(file.FullName))
                        {
                            var syntaxTree = VisualBasicSyntaxTree.ParseText(SourceText.From(stream), path: file.FullName);
                            trees.Add(syntaxTree);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }

            if (trees.Count != 0)
            {
                Compilation = (vbOptions != null)?
                        VisualBasicCompilation.Create(Project.AssemblyName,trees, meta, vbOptions):
                        (options!= null)? CSharpCompilation.Create(Project.AssemblyName, trees, meta, options) : null;
            }
        }
        private void SetSyntaxCompilation(List<MetadataReference> metadataReferences)
        {
            var trees = new List<SyntaxTree>();
            isSyntaxAnalysis = true;

            Logger.LogError(syntaxAnalysisError);
            Errors.Add(syntaxAnalysisError);

            var projPath = Path.GetDirectoryName(ProjectAnalyzer.ProjectFile.Path);
            DirectoryInfo directory = new DirectoryInfo(projPath);
            var extension = Path.GetExtension(ProjectAnalyzer.ProjectFile.Path);
            if (!string.IsNullOrEmpty(extension) && extension.Equals(".csproj", StringComparison.InvariantCultureIgnoreCase))
            {
                var allCsharpFiles = directory.GetFiles("*.cs", SearchOption.AllDirectories);
                foreach (var file in allCsharpFiles)
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
                        Logger.LogError(e, "Error while running CSharp syntax analysis");
                        Console.WriteLine(e);
                    }
                }
                if (trees.Count != 0)
                {
                    Compilation = CSharpCompilation.Create(ProjectAnalyzer.ProjectInSolution.ProjectName, trees, metadataReferences);
                }
            }
            else if (!string.IsNullOrEmpty(extension) && extension.Equals(".vbproj", StringComparison.InvariantCultureIgnoreCase))
            {
                var allVbFiles = directory.GetFiles("*.vb", SearchOption.AllDirectories);
                foreach (var file in allVbFiles)
                {
                    try
                    {
                        using (var stream = File.OpenRead(file.FullName))
                        {
                            var syntaxTree = VisualBasicSyntaxTree.ParseText(SourceText.From(stream), path: file.FullName);
                            trees.Add(syntaxTree);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Error while running VisualBasic syntax analysis");
                        Console.WriteLine(e);
                    }
                }

                if (trees.Count != 0)
                {
                    Compilation = VisualBasicCompilation.Create(ProjectAnalyzer.ProjectInSolution.ProjectName, trees, metadataReferences);
                }
            }
        }

        private Compilation CreateManualCompilation(string projectPath, List<string> references)
        {
            var trees = new List<SyntaxTree>();
            DirectoryInfo directory = new DirectoryInfo(Path.GetDirectoryName(projectPath));
            var extension = Path.GetExtension(projectPath);
            if (!string.IsNullOrEmpty(extension) && extension.Equals(".vbproj", StringComparison.InvariantCultureIgnoreCase))
            {
                var allFiles = directory.GetFiles("*.vb", SearchOption.AllDirectories);
                foreach (var file in allFiles)
                {
                    try
                    {
                        using (var stream = File.OpenRead(file.FullName))
                        {
                            var syntaxTree = VisualBasicSyntaxTree.ParseText(SourceText.From(stream), path: file.FullName);
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
                    return VisualBasicCompilation.Create(Path.GetFileNameWithoutExtension(projectPath), trees, references?.Select(r => MetadataReference.CreateFromFile(r)));
                }
            }
            else
            {
                var allCSharpFiles = directory.GetFiles("*.cs", SearchOption.AllDirectories);
                foreach (var file in allCSharpFiles)
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
                    return CSharpCompilation.Create(Path.GetFileNameWithoutExtension(projectPath), trees, references?.Select(r => MetadataReference.CreateFromFile(r)));
                }
            }
            return null;
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
        public ProjectBuildHandler(ILogger logger, AnalyzerConfiguration analyzerConfiguration = null, List<string> metaReferences = null)
        {
            Logger = logger;
            _analyzerConfiguration = analyzerConfiguration;
            _metaReferences = metaReferences;

            Errors = new List<string>();
            MissingMetaReferences = new List<string>();
        }
        public ProjectBuildHandler(ILogger logger, Project project, AnalyzerConfiguration analyzerConfiguration = null)
        {
            Logger = logger;
            _analyzerConfiguration = analyzerConfiguration;

            CompilationOptions options = project?.CompilationOptions;

            if (options is CSharpCompilationOptions)
            {
                /*
                 * This is to fix the compilation errors related to :
                 * Compile errors for assemblies which reference to mscorlib 2.0.5.0 (LINQPad 5.00.08)
                 * https://forum.linqpad.net/discussion/856/compile-errors-for-assemblies-which-reference-to-mscorlib-2-0-5-0-linqpad-5-00-08
                 */
                options = options.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);
            }

            this.Project = project?.WithCompilationOptions(options);
            _projectPath = project?.FilePath;
            Errors = new List<string>();
            MissingMetaReferences = new List<string>();
        }
        public ProjectBuildHandler(ILogger logger, Project project, Compilation compilation, Compilation preportCompilation, AnalyzerConfiguration analyzerConfiguration = null)
        {
            Logger = logger;
            _analyzerConfiguration = analyzerConfiguration;
            this.Project = project;
            _projectPath = project.FilePath;
            this.Compilation = compilation;
            this.PrePortCompilation = preportCompilation;
            Errors = new List<string>();
            MissingMetaReferences = new List<string>();
        }

        public ProjectBuildHandler(ILogger logger, string projectPath, List<string> oldReferences, List<string> references, AnalyzerConfiguration analyzerConfiguration = null)
        {
            Logger = logger;
            _analyzerConfiguration = analyzerConfiguration;
            _projectPath = projectPath;

            this.Compilation = CreateManualCompilation(projectPath, references);
            //We don't want a compilation if there are no older references, because it'll slow down the analysis
            this.PrePortCompilation = oldReferences?.Any() == true ? CreateManualCompilation(projectPath, oldReferences) : null;

            Errors = new List<string>();
            MissingMetaReferences = new List<string>();

            var errors = Compilation.GetDiagnostics()
               .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.GetMessage()?.Equals(KnownErrors.NoMainMethodMessage) != true);

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
                Logger.LogInformation($"{Compilation.AssemblyName} compiled with no errors");
            }
        }
        public async Task<ProjectBuildResult> Build()
        {
            await SetCompilation();
            ProjectBuildResult projectBuildResult = new ProjectBuildResult
            {
                BuildErrors = Errors,
                ProjectPath = _projectPath,
                ProjectRootPath = Path.GetDirectoryName(_projectPath),
                Project = Project,
                Compilation = Compilation,
                PrePortCompilation = PrePortCompilation,
                IsSyntaxAnalysis = isSyntaxAnalysis,
                PreportReferences = PrePortMetaReferences,
                MissingReferences = MissingMetaReferences
            };

            GetTargetFrameworks(projectBuildResult, AnalyzerResult);
            projectBuildResult.ProjectGuid = ProjectAnalyzer.ProjectGuid.ToString();
            projectBuildResult.ProjectType = ProjectAnalyzer.ProjectInSolution != null ? ProjectAnalyzer.ProjectInSolution.ProjectType.ToString() : string.Empty;
            
            foreach (var syntaxTree in Compilation.SyntaxTrees)
            {
                var sourceFilePath = Path.GetRelativePath(projectBuildResult.ProjectRootPath, syntaxTree.FilePath);
                var preportTree = PrePortCompilation?.SyntaxTrees?.FirstOrDefault(s => s.FilePath == syntaxTree.FilePath);
                var fileResult = new SourceFileBuildResult
                {
                    SyntaxTree = syntaxTree,
                    PrePortSemanticModel = preportTree != null ? PrePortCompilation?.GetSemanticModel(preportTree) : null,
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
                projectBuildResult.ExternalReferences = GetExternalReferences(
                    projectBuildResult?.Compilation,
                    projectBuildResult?.Project,
                    projectBuildResult?.Compilation?.References);
            }

            return projectBuildResult;
        }

        public ProjectBuildResult ReferenceOnlyBuild()
        {
            ProjectBuildResult projectBuildResult = new ProjectBuildResult
            {
                BuildErrors = Errors,
                ProjectPath = _projectPath,
                ProjectRootPath = Path.GetDirectoryName(_projectPath),
                Compilation = Compilation,
                PrePortCompilation = PrePortCompilation,
                IsSyntaxAnalysis = isSyntaxAnalysis,
                PreportReferences = PrePortMetaReferences,
                MissingReferences = MissingMetaReferences
            };

            GetTargetFrameworks(projectBuildResult, AnalyzerResult);
            projectBuildResult.ProjectGuid = ProjectAnalyzer.ProjectGuid.ToString();
            projectBuildResult.ProjectType = ProjectAnalyzer.ProjectInSolution != null ? ProjectAnalyzer.ProjectInSolution.ProjectType.ToString() : string.Empty;


            foreach (var syntaxTree in Compilation?.SyntaxTrees)
            {
                var sourceFilePath = Path.GetRelativePath(projectBuildResult.ProjectRootPath, syntaxTree.FilePath);
                var preportTree = PrePortCompilation?.SyntaxTrees?.FirstOrDefault(s => s.FilePath == syntaxTree.FilePath);
                var fileResult = new SourceFileBuildResult
                {
                    SyntaxTree = syntaxTree,
                    PrePortSemanticModel = preportTree != null ? PrePortCompilation?.GetSemanticModel(preportTree) : null,
                    SemanticModel = Compilation.GetSemanticModel(syntaxTree),
                    SourceFileFullPath = syntaxTree.FilePath,
                    SourceFilePath = sourceFilePath
                };
                projectBuildResult.SourceFileBuildResults.Add(fileResult);
                projectBuildResult.SourceFiles.Add(sourceFilePath);
            }

            if (_analyzerConfiguration != null && _analyzerConfiguration.MetaDataSettings.ReferenceData)
            {
                projectBuildResult.ExternalReferences = GetExternalReferences(Compilation, null, Compilation.References);
            }

            return projectBuildResult;
        }

        public async Task<ProjectBuildResult> IncrementalBuild(string filePath, ProjectBuildResult projectBuildResult)
        {
            await Task.Run(() =>
            {
                var languageVersion = LanguageVersion.Default;

                SyntaxTree updatedTree;
                var fileContents = File.ReadAllText(filePath);
                if (projectBuildResult.Compilation is CSharpCompilation compilation)
                {
                    languageVersion = compilation.LanguageVersion;
                    updatedTree = CSharpSyntaxTree.ParseText(SourceText.From(fileContents), path: filePath, options: new CSharpParseOptions(languageVersion));

                }
                else if (projectBuildResult.Compilation is VisualBasicCompilation vbCompilation)
                {
                    updatedTree = VisualBasicSyntaxTree.ParseText(SourceText.From(fileContents), path: filePath, options: new VisualBasicParseOptions(vbCompilation.LanguageVersion));
                }
                else
                {
                    // fall back to csharp to match old behavior.
                    updatedTree = CSharpSyntaxTree.ParseText(SourceText.From(fileContents), path: filePath, options: new CSharpParseOptions(languageVersion));
                }
                
                var syntaxTree = Compilation.SyntaxTrees.FirstOrDefault(syntaxTree => syntaxTree.FilePath == filePath);
                var preportSyntaxTree = Compilation.SyntaxTrees.FirstOrDefault(syntaxTree => syntaxTree.FilePath == filePath);

                Compilation = Compilation.RemoveSyntaxTrees(syntaxTree).AddSyntaxTrees(updatedTree);
                PrePortCompilation = PrePortCompilation?.RemoveSyntaxTrees(preportSyntaxTree).AddSyntaxTrees(updatedTree);

                var oldSourceFileBuildResult = projectBuildResult.SourceFileBuildResults.FirstOrDefault(sourceFile => sourceFile.SourceFileFullPath == filePath);
                projectBuildResult.SourceFileBuildResults.Remove(oldSourceFileBuildResult);

                var sourceFilePath = Path.GetRelativePath(projectBuildResult.ProjectRootPath, filePath);
                var preportTree = PrePortCompilation?.SyntaxTrees?.FirstOrDefault(s => s.FilePath == syntaxTree.FilePath);
                var fileResult = new SourceFileBuildResult
                {
                    SyntaxTree = updatedTree,
                    PrePortSemanticModel = preportTree != null ? PrePortCompilation?.GetSemanticModel(preportTree) : null,
                    SemanticModel = Compilation.GetSemanticModel(updatedTree),
                    SourceFileFullPath = syntaxTree.FilePath,
                    SyntaxGenerator = SyntaxGenerator.GetGenerator(Project),
                    SourceFilePath = sourceFilePath
                };

                projectBuildResult.SourceFileBuildResults.Add(fileResult);
                projectBuildResult.Compilation = Compilation;
                projectBuildResult.PrePortCompilation = PrePortCompilation;
            });


            return projectBuildResult;
        }

        public ProjectBuildResult SyntaxOnlyBuild()
        {
            return SyntaxOnlyBuild(null);
        }

        public ProjectBuildResult SyntaxOnlyBuild(Dictionary<string, MetadataReference> metadataReferences)
        {
            SetSyntaxCompilation(metadataReferences?.Values?.ToList());

            ProjectBuildResult projectBuildResult = new ProjectBuildResult
            {
                BuildErrors = Errors,
                ProjectPath = ProjectAnalyzer.ProjectFile.Path,
                ProjectRootPath = Path.GetDirectoryName(ProjectAnalyzer.ProjectFile.Path),
                Compilation = Compilation,
                IsSyntaxAnalysis = isSyntaxAnalysis,
                ExternalReferences = new ExternalReferences()
                {
                    ProjectReferences = metadataReferences?.Select(m=> new ExternalReference()
                        {
                            Identity = m.Value.Display,
                            AssemblyLocation = m.Key
                        }).ToList()
                }
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
            if (analyzerResult != null)
            {
                result.TargetFramework = analyzerResult.TargetFramework;
                var targetFrameworks = analyzerResult.GetProperty(Constants.TargetFrameworks);
                if (!string.IsNullOrEmpty(targetFrameworks))
                {
                    result.TargetFrameworks = targetFrameworks.Split(';').ToList();
                }
            }
            else
            {
                result.TargetFramework = ProjectAnalyzer.ProjectFile.TargetFrameworks.FirstOrDefault();
                result.TargetFrameworks = ProjectAnalyzer.ProjectFile.TargetFrameworks.ToList();
            }
        }

        private ExternalReferences GetExternalReferences(
            Compilation compilation,
            Project project,
            IEnumerable<MetadataReference> externalReferencesMetaData)
        {
            ExternalReferenceLoader externalReferenceLoader = new ExternalReferenceLoader(
                Directory.GetParent(_projectPath).FullName,
                compilation, 
                project, 
                AnalyzerResult?.PackageReferences, 
                Logger);

            return externalReferenceLoader.Load();
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
