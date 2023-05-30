using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging.Abstractions;

namespace Codelyzer.Analysis.Common
{
    public class ProjectBuildHelper
    {
        private readonly ILogger _logger;

        public ProjectBuildHelper(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets pre-port compilation, pre-port meta references, and missing references from given project
        /// </summary>
        /// <returns>(Pre-port Compilation, Pre-port meta references, MissingMetaReferences)</returns>
        public async Task<(Compilation?, List<string>, List<string>)> GetPrePortCompilation(Project project)
        {
            var projectBuildHelper = new ProjectBuildHelper(_logger);
            var projectFile = projectBuildHelper.LoadProjectFile(project.FilePath);
            if (projectFile == null)
            {
                return (null, new List<string?>(), new List<string>());
            }
            var (prePortReferences, missingMetaReferences) = 
                projectBuildHelper.LoadMetadataReferences(projectFile);
            if (prePortReferences.Count > 0)
            {
                var prePortProject = project.WithMetadataReferences(prePortReferences);
                var prePortMetaReferences = prePortReferences
                    .Select(m => m.Display ?? "")
                    .ToList();
                var prePortCompilation = await prePortProject.GetCompilationAsync();
                return (prePortCompilation, prePortMetaReferences, missingMetaReferences);
            }
            return (null, new List<string>(), missingMetaReferences);
        }

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
                _logger.LogError(ex, "Error loading project file {}", projectFilePath);
                return null;
            }
        }

        public (List<PortableExecutableReference>, List<string>) LoadMetadataReferences(
            XDocument projectFile)
        {
            var references = new List<PortableExecutableReference>();
            var missingMetaReferences = new List<string>();

            if (projectFile == null)
            {
                return (references, missingMetaReferences);
            }

            var fileReferences = ExtractFileReferencesFromProject(projectFile);
            foreach (var fileRef in fileReferences)
            {
                if (!File.Exists(fileRef))
                {
                    missingMetaReferences.Add(fileRef);
                    _logger.LogWarning("Assembly {} referenced does not exist.", fileRef);
                }
                try
                {
                    references.Add(MetadataReference.CreateFromFile(fileRef));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while parsing metadata reference {}.", fileRef);
                }
            }
            return (references, missingMetaReferences);
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
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)?
                .Where(s => !(s.Contains("<!-") || s.Contains("-->")))
                .Select(s => s.Trim())
                .ToList() ?? new List<string>();

            return fileReferences;
        }

        public Dictionary<string, ProjectInSolution> GetProjectInSolutionObjects(string solutionPath)
        {
            var map = new Dictionary<string, ProjectInSolution>();
            var solution = SolutionFile.Parse(solutionPath);
            return solution.ProjectsInOrder.ToDictionary(project => project.ProjectName);
        }
        
        public ExternalReferences GetExternalReferences(
            Compilation compilation,
            Project project)
        {
            if (project.FilePath == null)
            {
                // todo: error metric candidate
                throw new Exception("Project file path is invalid");
            }
            ExternalReferenceLoader externalReferenceLoader = new ExternalReferenceLoader(
                Directory.GetParent(project.FilePath)?.FullName,
                compilation,
                project,
                new Dictionary<string, IReadOnlyDictionary<string, string>>(),
                NullLogger.Instance);

            return externalReferenceLoader.Load();
        }
    }
}
