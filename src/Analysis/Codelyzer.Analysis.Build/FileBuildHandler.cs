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
        private Compilation _compilation;
        private Compilation _prePortCompilation;

        private List<string> Errors { get; set; }
        private ILogger Logger;

        private string _projectPath;
        private Dictionary<string, string> _fileInfo;
        private IEnumerable<PortableExecutableReference> _frameworkMetaReferences;
        private IEnumerable<PortableExecutableReference> _coreMetaReferences;

        public FileBuildHandler(ILogger logger, string projectPath, Dictionary<string, string> fileInfo, List<string> frameworkMetaReferences, List<string> coreMetaReferences)
        {
            Logger = logger;
            _fileInfo = fileInfo;
            _frameworkMetaReferences = frameworkMetaReferences?.Select(m => MetadataReference.CreateFromFile(m));
            _coreMetaReferences = coreMetaReferences?.Select(m => MetadataReference.CreateFromFile(m));
            _projectPath = projectPath;

            Errors = new List<string>();
        }

        public FileBuildHandler(ILogger logger, string projectPath, Dictionary<string, string> fileInfo, IEnumerable<PortableExecutableReference> frameworkMetaReferences, IEnumerable<PortableExecutableReference> coreMetaReferences)
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
            var results = new List<SourceFileBuildResult>();
            var trees = new List<SyntaxTree>();

            await Task.Run(() =>
            {
                foreach (var file in _fileInfo)
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
                        _prePortCompilation = CSharpCompilation.Create(projectName, trees, _frameworkMetaReferences);
                    }
                    if (_coreMetaReferences?.Any() == true)
                    {
                        _compilation = CSharpCompilation.Create(projectName, trees, _coreMetaReferences);
                    }
                }


                _fileInfo.Keys.ToList().ForEach(file =>
                {
                    var sourceFilePath = Path.GetRelativePath(_projectPath, file);
                    var fileTree = trees.FirstOrDefault(t => t.FilePath == file);

                    if (fileTree != null)
                    {
                        var fileResult = new SourceFileBuildResult
                        {
                            SyntaxTree = fileTree,
                            PrePortSemanticModel = _prePortCompilation?.GetSemanticModel(fileTree),
                            SemanticModel = _compilation?.GetSemanticModel(fileTree),
                            SourceFileFullPath = file,
                            SourceFilePath = file
                        };
                        results.Add(fileResult);
                    }
                    else
                    {
                        Logger.LogError($"Cannot find a syntax tree for {file}");
                    }
                });
            });
            return results;
        }  

        public void Dispose()
        {
            _compilation = null;
            Logger = null;
        }
    }
}
