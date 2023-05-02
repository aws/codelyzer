using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging.Abstractions;

namespace Codelyzer.Analysis.Common
{
    public class ProjectBuildHelper
    {
        public XDocument LoadProjectFile(string projectFilePath)
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
                Console.WriteLine(ex);
                //todo: emit error metric
                throw;
            }
        }
        public List<PortableExecutableReference> LoadMetadataReferences
            (XDocument projectFile,
                out List<string> missingMetaReferences)
        {
            var references = new List<PortableExecutableReference>();
            missingMetaReferences = new List<string>();

            if (projectFile == null)
            {
                return references;
            }

            var fileReferences = ExtractFileReferencesFromProject(projectFile);
            foreach (var fileRef in fileReferences)
            {
                if (!File.Exists(fileRef))
                {
                    missingMetaReferences.Add(fileRef);
                    //Logger.LogWarning("Assembly {} referenced does not exist.", fileRef);
                }
                try
                {
                    references.Add(MetadataReference.CreateFromFile(fileRef));
                }
                catch (Exception ex)
                {
                    //Logger.LogError(ex, "Error while parsing metadata reference {}.", fileRef);
                }
            }
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
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)?
                .Where(s => !(s.Contains("<!-") || s.Contains("-->")))
                .Select(s => s.Trim())
                .ToList();

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
