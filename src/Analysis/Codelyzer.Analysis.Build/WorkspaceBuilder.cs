using Codelyzer.Analysis.Common;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codelyzer.Analysis.Model.Build;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using System.Diagnostics;
using Codelyzer.Analysis.Model;
using System.Xml;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Codelyzer.Analysis.Build
{
    public class WorkspaceBuilder
    {
        private readonly ILogger Logger;
        private readonly AnalyzerConfiguration _analyzerConfiguration;
        private readonly string _workspacePath;

        private List<ProjectBuildResult> ProjectResults { get; }


        public WorkspaceBuilder(ILogger logger, string workspacePath, AnalyzerConfiguration analyzerConfiguration = null)
        {
            this.ProjectResults = new List<ProjectBuildResult>();
            this._workspacePath = workspacePath;
            this.Logger = logger;
            _analyzerConfiguration = analyzerConfiguration;
        }
       
        public async IAsyncEnumerable<ProjectBuildResult> BuildProject()
        {         
            using (var builder = new WorkspaceBuilderHelper(Logger, _workspacePath, _analyzerConfiguration))
            {
                try
                {
                    if (!_analyzerConfiguration.BuildSettings.SyntaxOnly)
                    {
                        var projectResultEnumerator = builder.BuildProjectIncremental().GetAsyncEnumerator();
                        while (await projectResultEnumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            var result = projectResultEnumerator.Current;
                            if (result?.AnalyzerResult != null)
                            {
                                using (ProjectBuildHandler projectBuildHandler = new ProjectBuildHandler(Logger, result.Project, _analyzerConfiguration))
                                {
                                    projectBuildHandler.AnalyzerResult = result.AnalyzerResult;
                                    projectBuildHandler.ProjectAnalyzer = result.ProjectAnalyzer;
                                    var projectBuildResult = await projectBuildHandler.Build();
                                    yield return projectBuildResult;
                                }
                            }
                            else
                            {
                                if (_analyzerConfiguration.AnalyzeFailedProjects)
                                {
                                    using (ProjectBuildHandler projectBuildHandler = new ProjectBuildHandler(Logger, _analyzerConfiguration))
                                    {
                                        projectBuildHandler.ProjectAnalyzer = result.ProjectAnalyzer;
                                        var projectBuildResult = projectBuildHandler.SyntaxOnlyBuild();
                                        yield return projectBuildResult;
                                    }
                                }
                            }
                        }
                        await projectResultEnumerator.DisposeAsync();
                    }
                    else 
                    {
                        var projectReferencesMap = FileUtils.GetProjectsWithReferences(_workspacePath);
                        builder.GenerateNoBuildAnalysis();

                        var projectsInOrder = CreateDependencyQueue(projectReferencesMap);
                        Dictionary<string, MetadataReference> references = new Dictionary<string, MetadataReference>();

                        foreach (string projectPath in projectsInOrder)
                        {
                            var project = builder.Projects.Find(p => p.ProjectAnalyzer.ProjectFile.Path.Equals(projectPath, StringComparison.InvariantCultureIgnoreCase));
                            var projectReferencePaths = projectReferencesMap[projectPath]?.Distinct().ToHashSet<string>();

                            using (ProjectBuildHandler projectBuildHandler =
                                   new ProjectBuildHandler(Logger, project.Project, _analyzerConfiguration))
                            {
                                projectBuildHandler.AnalyzerResult = project.AnalyzerResult;
                                projectBuildHandler.ProjectAnalyzer = project.ProjectAnalyzer;
                                var projectReferences = references.Where(r => projectReferencePaths.Contains(r.Key)).ToDictionary(p => p.Key, p => p.Value);
                                var result = projectBuildHandler.SyntaxOnlyBuild(projectReferences);
                                if (result != null)
                                {
                                    references.Add(projectPath, result.Compilation.ToMetadataReference());
                                    yield return result;
                                }
                            }
                        }
                    }
                }
                finally
                {
                }
            }
        }

        public async Task<List<ProjectBuildResult>> Build()
        {
            using (var builder = new WorkspaceBuilderHelper(Logger, _workspacePath, _analyzerConfiguration))
            {
                if (!_analyzerConfiguration.BuildSettings.SyntaxOnly)
                {
                    builder.Build();
                    foreach (var projectResult in builder.Projects)
                    {
                        using (ProjectBuildHandler projectBuildHandler =
                            new ProjectBuildHandler(Logger, projectResult.Project, _analyzerConfiguration))
                        {
                            projectBuildHandler.AnalyzerResult = projectResult.AnalyzerResult;
                            projectBuildHandler.ProjectAnalyzer = projectResult.ProjectAnalyzer;
                            var result = await projectBuildHandler.Build();
                            ProjectResults.Add(result);
                        }
                    }
                    if (_analyzerConfiguration.AnalyzeFailedProjects)
                    {
                        foreach (var projectResult in builder.FailedProjects)
                        {
                            using (ProjectBuildHandler projectBuildHandler =
                            new ProjectBuildHandler(Logger, _analyzerConfiguration))
                            {
                                projectBuildHandler.ProjectAnalyzer = projectResult.ProjectAnalyzer;
                                var result = projectBuildHandler.SyntaxOnlyBuild();
                                ProjectResults.Add(result);
                            }
                        }
                    }
                } 
                else
                {
                    var projectReferencesMap = FileUtils.GetProjectsWithReferences(_workspacePath);
                    builder.GenerateNoBuildAnalysis();

                    var projectsInOrder = CreateDependencyQueue(projectReferencesMap);
                    Dictionary<string, MetadataReference> references = new Dictionary<string, MetadataReference>();

                    while (projectsInOrder.Count > 0)
                    {
                        var projectPath = projectsInOrder.Dequeue();
                        var project = builder.Projects.Find(p => p.ProjectAnalyzer.ProjectFile.Path.Equals(projectPath, StringComparison.InvariantCultureIgnoreCase));
                        var projectReferencePaths = projectReferencesMap[projectPath]?.Distinct().ToHashSet<string>();

                        using (ProjectBuildHandler projectBuildHandler =
                            new ProjectBuildHandler(Logger, project.Project, _analyzerConfiguration))
                        {
                            projectBuildHandler.AnalyzerResult = project.AnalyzerResult;
                            projectBuildHandler.ProjectAnalyzer = project.ProjectAnalyzer;
                            var projectReferences = references.Where(r => projectReferencePaths.Contains(r.Key)).ToDictionary(p=>p.Key, p=> p.Value);
                            var result = projectBuildHandler.SyntaxOnlyBuild(projectReferences);
                            if (result != null)
                            {
                                references.Add(projectPath, result.Compilation.ToMetadataReference());
                                ProjectResults.Add(result);
                            }
                        }
                    }
                }
            }

            return ProjectResults;
        }

        private Queue<string> CreateDependencyQueue(Dictionary<string, HashSet<string>> projectReferencesMap)
        {
            var projectsInOrder = new Queue<string>();
            var builtProjects = new HashSet<string>();
            foreach (var project in projectReferencesMap.Keys)
            {
                try
                {
                    if (!builtProjects.Contains(project))
                    {
                        CreateDependencyQueueHelper(project, builtProjects, projectReferencesMap, projectsInOrder);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error while getting dependencies for project {project}");
                }
            }
            return projectsInOrder;
        }

        private void CreateDependencyQueueHelper(string projectPath, HashSet<string> builtProjects, Dictionary<string, HashSet<string>> projectReferencesMap, Queue<string> buildOrder)
        {
            if (projectReferencesMap.ContainsKey(projectPath))
            {
                builtProjects.Add(projectPath);
                foreach (var dependency in projectReferencesMap[projectPath])
                {
                    if (!builtProjects.Contains(dependency))
                        CreateDependencyQueueHelper(dependency, builtProjects, projectReferencesMap, buildOrder);
                }
                buildOrder.Enqueue(projectPath);
            }
            else
            {
                Logger.LogInformation($"Missing project found in references {projectPath}");
            }
        }

        public List<ProjectBuildResult> GenerateNoBuildAnalysis(Dictionary<string, List<string>> oldReferences, Dictionary<string, List<string>> references)
        {
            using (var builder = new WorkspaceBuilderHelper(Logger, _workspacePath, _analyzerConfiguration))
            {
                builder.GenerateNoBuildAnalysis();

                foreach (var projectResult in builder.Projects)
                {
                    var projectPath = projectResult.ProjectAnalyzer.ProjectFile.Path;
                    var oldRefs = oldReferences?.ContainsKey(projectPath) == true ? oldReferences[projectPath] : null;
                    var refs = references?.ContainsKey(projectPath) == true ? references[projectPath] : null;
                    using (ProjectBuildHandler projectBuildHandler =
                    new ProjectBuildHandler(Logger, projectPath, oldRefs, refs, _analyzerConfiguration))
                    {
                        projectBuildHandler.ProjectAnalyzer = projectResult.ProjectAnalyzer;
                        var result = projectBuildHandler.ReferenceOnlyBuild();
                        ProjectResults.Add(result);
                    }
                    
                }
            }
            return ProjectResults;
        }

        public List<ProjectBuildResult> BuildLiteBuildAnalysis(Dictionary<string, List<string>> references)
        {

            List<ProjectBuildResult> projectBuildResults = null;
            using (var builder = new WorkspaceBuilderHelper(Logger, _workspacePath, _analyzerConfiguration))
            {
                builder.GenerateNoBuildAnalysis();
                var workspace = CreateAdhocWorkspaceForLiteBuild(references, builder.Projects);
                projectBuildResults= GenerateProjectBuildResultsWithAdHocWorkspace(workspace, builder.Projects);
            }
            return projectBuildResults;
        }
        public AdhocWorkspace CreateAdhocWorkspaceForLiteBuild(Dictionary<string, List<string>> references, List<ProjectAnalysisResult> projectAnalysisResults)
        {
            try
            {
                var projectReferencesMap = FileUtils.GetProjectsWithReferences(_workspacePath);
                var projectsInOrder = CreateDependencyQueue(projectReferencesMap);

                AdhocWorkspace adhocWorkspace = new AdhocWorkspace();

                foreach (var projectPath in projectsInOrder)
                {
                    var projectAnalysisResult = projectAnalysisResults.FirstOrDefault(c => c.ProjectAnalyzer.ProjectFile.Path.Equals(projectPath, StringComparison.InvariantCultureIgnoreCase));

                    List<MetadataReference> metadataReferencesList = new List<MetadataReference>();
                    if (!references.Any(i => i.Key.ToLower().Contains(projectPath))) continue;

                    var fromInputReferences = references.FirstOrDefault(i => i.Key.ToLower().Contains(projectPath)).Value;

                    foreach (string filePath in fromInputReferences)
                    {
                        metadataReferencesList.Add(MetadataReference.CreateFromFile(filePath));
                    }
                    var projectId = projectAnalysisResult == null ? ProjectId.CreateFromSerialized(Guid.NewGuid()) : 
                        ProjectId.CreateFromSerialized(projectAnalysisResult.ProjectAnalyzer.ProjectGuid);
                    var projectName = projectAnalysisResult.ProjectAnalyzer.ProjectInSolution.ProjectName;
                    var codeFiles = GetCodeFiles(projectPath);
                    List<DocumentInfo> docInfoList = new List<DocumentInfo>();
                    foreach (var doc in codeFiles)
                    {
                        try
                        {
                            DocumentInfo docInfo = DocumentInfo.Create(
                                DocumentId.CreateFromSerialized(projectId, Guid.NewGuid()),
                                projectName,
                                loader: TextLoader.From(
                                    TextAndVersion.Create(
                                        SourceText.From(File.ReadAllText(doc), Encoding.Unicode), VersionStamp.Create())),
                                filePath: doc);

                            docInfoList.Add(docInfo);
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine(e.ToString());
                        }
                    }

                    var projectInfo = ProjectInfo.Create(
                        projectId,
                        VersionStamp.Create(),
                        name: projectName,
                        language: "C#", // doesn't matter for VB or C#
                        assemblyName: projectName,
                        filePath: projectPath,
                        outputFilePath: Path.Combine(projectPath,"bin"),
                        projectReferences: null,//curProject.projectReferences,
                        metadataReferences: metadataReferencesList,
                        documents: docInfoList
                        );


                    var solution = adhocWorkspace.CurrentSolution.AddProject(projectInfo);

                    if (!adhocWorkspace.TryApplyChanges(solution))
                    {
                        return null;
                    }
                }

                // reapply project reference on compilation 
                foreach (var projPath in projectsInOrder)
                {
                    var referenceHashMap = new HashSet<string>();
                    projectReferencesMap.TryGetValue(projPath, out referenceHashMap);
                    if (referenceHashMap.Count > 0)
                    {
                        var mainProjectInfo = adhocWorkspace.CurrentSolution.Projects.FirstOrDefault(c => c.FilePath.Equals(projPath, StringComparison.InvariantCultureIgnoreCase));
                        var referenceProjInfoList = new List<ProjectReference>();
                        foreach (var referenceProjPath in referenceHashMap)
                        {
                            var referenceProjectInfo = adhocWorkspace.CurrentSolution.Projects.FirstOrDefault(c => c.FilePath.Equals(referenceProjPath,StringComparison.InvariantCultureIgnoreCase));
                            if (referenceProjectInfo != null)
                            {
                                referenceProjInfoList.Add(new ProjectReference(referenceProjectInfo.Id));
                            }
                        }
                        var solution = adhocWorkspace.CurrentSolution.WithProjectReferences(mainProjectInfo.Id, referenceProjInfoList.AsEnumerable());
                        if (!adhocWorkspace.TryApplyChanges(solution))
                        {
                            return null;
                        }
                    }
                }
                return adhocWorkspace;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private List <string > GetCodeFiles(string projectPath)
        {
            List<string> allFiles = new List<string>();
            DirectoryInfo directory = new DirectoryInfo(Path.GetDirectoryName(projectPath));
            var extension = Path.GetExtension(projectPath);
            if (!string.IsNullOrEmpty(extension) && extension.Equals(".vbproj", StringComparison.InvariantCultureIgnoreCase))
            {
                allFiles = FileUtils.GetProjectCodeFiles(projectPath, directory.FullName,"*.vb").ToList();
            }
            else if(!string.IsNullOrEmpty(extension) && extension.Equals(".csproj", StringComparison.InvariantCultureIgnoreCase))
            {
                allFiles = FileUtils.GetProjectCodeFiles(projectPath, directory.FullName,"*.cs").ToList();
            }
            return allFiles;
        }


        public List<ProjectBuildResult> GenerateProjectBuildResultsWithAdHocWorkspace(AdhocWorkspace workspace, List<ProjectAnalysisResult> projectAnalysisResults)
        {
            List<ProjectBuildResult> projectBuildResults = new List<ProjectBuildResult>();
            Console.WriteLine("Workspace.CurrentSolution.Projects Count :" + (workspace.CurrentSolution.Projects.Count()));
            foreach (Project project in workspace.CurrentSolution.Projects)
            {
                var projectAnalysisResult = projectAnalysisResults.FirstOrDefault(c => c.ProjectAnalyzer.ProjectFile.Path.Equals(project.FilePath, StringComparison.InvariantCultureIgnoreCase));

                try
                {
                    Console.WriteLine("GetExternalReferences for project: " + project.FilePath);
                    ProjectBuildResult projectBuildResult = new ProjectBuildResult();
                    projectBuildResult.Project = project;
                    projectBuildResult.ProjectPath = projectAnalysisResult.ProjectAnalyzer.ProjectInSolution.AbsolutePath;
                    projectBuildResult.ProjectRootPath = Path.GetDirectoryName(projectBuildResult.ProjectPath);
                    projectBuildResult.ProjectGuid = project.Id.Id.ToString();
                    projectBuildResult.BuildErrors = new List<string>();
                    projectBuildResult.Compilation = CSharpCompilation.Create(null).AddReferences(project.MetadataReferences);
                    projectBuildResult.ExternalReferences = new ProjectBuildHandler().GetExternalReferences(projectBuildResult.Compilation, project, project.MetadataReferences);
                    projectBuildResult.ProjectType = projectAnalysisResult.ProjectAnalyzer.ProjectInSolution.ProjectType.ToString();
                    projectBuildResult.TargetFramework = projectAnalysisResult.ProjectAnalyzer.ProjectFile.TargetFrameworks.FirstOrDefault();

                    var validSourceDocs = project.Documents.Where(d => !d.FilePath.EndsWith("cshtml.g.cs"));
                    foreach (var document in validSourceDocs)
                    {
                        SourceFileBuildResult sourceFileBuildResult = new SourceFileBuildResult();
                        sourceFileBuildResult.SourceFileFullPath = document.FilePath;
                        sourceFileBuildResult.SourceFilePath = document.Name;
                        sourceFileBuildResult.SyntaxTree = document.GetSyntaxTreeAsync().Result;
                        //The Compilation creates the SemanticModel from the SyntaxTree. 
                        sourceFileBuildResult.SemanticModel = document.GetSemanticModelAsync().Result;
                        projectBuildResult.SourceFileBuildResults.Add(sourceFileBuildResult);
                        projectBuildResult.SourceFiles.Add(sourceFileBuildResult.SourceFileFullPath);
                    }

                    projectBuildResults.Add(projectBuildResult);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "BuildUsingAdHocWorkspace Error");
                }
            }
            
            return projectBuildResults;
        }


    }
}
