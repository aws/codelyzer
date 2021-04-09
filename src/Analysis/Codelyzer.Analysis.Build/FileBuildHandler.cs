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
using JetBrains.Profiler.Api;

namespace Codelyzer.Analysis.Build
{
    public class FileBuildHandler : IDisposable
    {
        private Project Project;
        private Compilation Compilation;
        private Compilation PrePortCompilation;

        private List<string> Errors { get; set; }
        private ILogger Logger;
        private readonly AnalyzerConfiguration _analyzerConfiguration;
        internal IAnalyzerResult AnalyzerResult;
        internal IProjectAnalyzer ProjectAnalyzer;
        internal bool isSyntaxAnalysis;

        private string _projectPath;
        private List<string> _files;
        private List<string> _frameworkMetaReferences;
        private List<string> _coreMetaReferences;

        public FileBuildHandler(ILogger logger, string projectPath, List<string> files, List<string> frameworkMetaReferences, List<string> coreMetaReferences)
        {
            Logger = logger;
            _files = files;
            _frameworkMetaReferences = frameworkMetaReferences;
            _coreMetaReferences = coreMetaReferences;
            _projectPath = projectPath;

            Errors = new List<string>();
        }
       public async Task<List<SourceFileBuildResult>> Build()
        {
            var trees = new List<SyntaxTree>();
            foreach(var file in _files)
            {
                var fileContent = File.ReadAllText(file);
                var syntaxTree = CSharpSyntaxTree.ParseText(fileContent, path: file);
                trees.Add(syntaxTree);
            }
            if (trees.Count != 0)
            {
                var projectName = Path.GetFileNameWithoutExtension(_projectPath);

                if (_frameworkMetaReferences?.Any() == true)
                {
                    PrePortCompilation = CSharpCompilation.Create(projectName, trees, _frameworkMetaReferences.Select(m => MetadataReference.CreateFromFile(m)));
                }
                if (_coreMetaReferences?.Any() == true)
                {
                    Compilation = CSharpCompilation.Create(projectName, trees, _coreMetaReferences.Select(m => MetadataReference.CreateFromFile(m)));
                }
            }

            var results = new List<SourceFileBuildResult>();

            _files.ForEach(file => {
                var sourceFilePath = Path.GetRelativePath(_projectPath, file);
                var fileTree = trees.FirstOrDefault(t => t.FilePath == file);

                var fileResult = new SourceFileBuildResult
                {
                    SyntaxTree = fileTree,
                    PrePortSemanticModel = PrePortCompilation?.GetSemanticModel(fileTree),
                    SemanticModel = Compilation.GetSemanticModel(fileTree),
                    SourceFileFullPath = file,
                    SourceFilePath = file
                };

                results.Add(fileResult);
            });

            return results;
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
