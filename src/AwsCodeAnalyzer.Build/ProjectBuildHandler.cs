using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Serilog;

namespace AwsCodeAnalyzer.Build
{
    public class SourceFileBuildResult
    {
        public SyntaxTree SyntaxTree { get; set; }
        public SemanticModel SemanticModel { get; set; }
        public string SourceFileFullPath { get; set; }         
        public string SourceFilePath { get; set; }
        public SyntaxGenerator SyntaxGenerator { get; set; }
    }
    
    public  class ProjectBuildResult
    { 
        public string ProjectPath { get; set; }
        
        public string ProjectRootPath { get; set; }
        public List<string> SourceFiles { get; private set; }
        public List<SourceFileBuildResult> SourceFileBuildResults { get; private set; }
        public List<string> BuildErrors { get; set; }
        public Project Project { get; set; }
        public string TargetFramework { get; set; }

        public ProjectBuildResult()
        {
            SourceFileBuildResults = new List<SourceFileBuildResult>();
            SourceFiles = new List<string>();
        }

        public bool IsBuildSuccess()
        {
            return BuildErrors.Count == 0;
        }

        internal void AddSourceFile(string filePath)
        {
            var wsPath = Path.GetRelativePath(ProjectRootPath, filePath);
            SourceFiles.Add(wsPath);
        }
    }
    public class ProjectBuildHandler
    {
        private readonly Project Project;
        private Compilation Compilation;
        private List<string> Errors { get; set; }
        private readonly ILogger Logger;

        private async Task SetCompilation()
        {
            Compilation = await Project.GetCompilationAsync();
            var errors = Compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            if (errors.Any())
            {
                Logger.Error($"COMPILATION ERROR: {Compilation.AssemblyName}: {errors.Count()} " +
                                $"compilation errors: \n\t{string.Join("\n\t", errors.Where(e => false).Select(e => e.ToString()))}");
                Logger.Debug(String.Join("\n", errors));

                foreach (var error in errors)
                {
                    Errors.Add(error.ToString());
                }
            }
            else
            {
                Logger.Information($"Project {Project.Name} compiled with no errors");
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

        public ProjectBuildHandler(ILogger logger, Project project)
        {
            Logger = logger;
            
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
                Project = Project
            };

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

            return projectBuildResult;
        }
    }
}
