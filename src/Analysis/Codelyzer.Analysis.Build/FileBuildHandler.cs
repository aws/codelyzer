using Buildalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Codelyzer.Analysis.Build
{
    public class FileBuildHandler : IDisposable
    {
        private Compilation Compilation;
        private Compilation PrePortCompilation;

        private List<string> Errors { get; set; }
        private ILogger Logger;

        private string _projectPath;
        private Dictionary<string, string> _fileInfo;
        private List<string> _frameworkMetaReferences;
        private List<string> _coreMetaReferences;

        public FileBuildHandler(ILogger logger, string projectPath, Dictionary<string, string> fileInfo, List<string> frameworkMetaReferences, List<string> coreMetaReferences)
        {
            Logger = logger;
            _fileInfo = fileInfo;
            _frameworkMetaReferences = frameworkMetaReferences;
            _coreMetaReferences = coreMetaReferences;
            _projectPath = projectPath;

            Errors = new List<string>();
        }
       public async Task<List<SourceFileBuildResult>> Build()
        {
            var trees = new List<SyntaxTree>();
            foreach(var file in _fileInfo)
            {
                var fileContent = file.Value;
                var syntaxTree = CSharpSyntaxTree.ParseText(fileContent, path: file.Key);
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

            _fileInfo.Keys.ToList().ForEach(file => {
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
            Logger = null;
        }
    }
}
