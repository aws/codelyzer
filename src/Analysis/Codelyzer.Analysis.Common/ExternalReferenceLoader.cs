using Codelyzer.Analysis.Model;
using Codelyzer.Analysis.Model.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Codelyzer.Analysis.Common

{
    public class ExternalReferenceLoader
    {
        private readonly string _projectDir;
        private readonly Compilation _compilation;
        private readonly Project _project;
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _packageReferences;
        private readonly ILogger Logger;

        private ExternalReferences _externalReferences;

        public ExternalReferenceLoader(string projectDir, Compilation compilation, Project project, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> packageReferences, ILogger logger)
        {
            _externalReferences = new ExternalReferences();

            _projectDir = projectDir;
            _compilation = compilation;
            _project = project;
            _packageReferences = packageReferences;
            Logger = logger;
        }

        public ExternalReferences Load()
        {
            var projectReferenceNames = LoadProjectReferences();
            LoadFromBuildPackageReferences();
            LoadFromPackagesConfig();
            LoadFromCompilation(projectReferenceNames);
            return _externalReferences;
        }

        private HashSet<string> LoadProjectReferences()
        {
            var projectReferenceNames = new HashSet<string>();
            if (_project != null)
            {
                var projectReferencesIds = _project.ProjectReferences != null ? _project.ProjectReferences.Select(pr => pr.ProjectId).ToList() : null;
                var projectReferences = projectReferencesIds != null ? _project.Solution.Projects.Where(p => projectReferencesIds.Contains(p.Id)) : null;
                projectReferenceNames = projectReferences != null ? projectReferences.Select(p => p.Name).ToHashSet() : null;

                _externalReferences.ProjectReferences.AddRange(projectReferences.Select(p => new ExternalReference()
                {
                    Identity = p.Name,
                    AssemblyLocation = p.FilePath,
                    Version = p.Version.ToString()
                }));
            }
            return projectReferenceNames;
        }

        private void LoadFromBuildPackageReferences()
        {
            _packageReferences?.ToList().ForEach(packageReference =>
            {
                bool getVersionSuccess = packageReference.Value.TryGetValue(Constants.Version, out var version);
                var reference = new ExternalReference()
                {
                    Identity = packageReference.Key,
                    Version = getVersionSuccess ? version : "",
                };
                if (!_externalReferences.NugetReferences.Contains(reference))
                {
                    _externalReferences.NugetReferences.Add(reference);
                }
            });
        }

        private void LoadFromPackagesConfig()
        {
            IEnumerable<PackageReference> packageReferences = new List<PackageReference>();

            string packageConfig = Path.Combine(_projectDir, Constants.PackagesConfig);
            if (File.Exists(packageConfig))
            {
                try
                {
                    using (var fileStream = new FileStream(packageConfig, FileMode.Open))
                    {
                        var configReader = new PackagesConfigReader(fileStream);
                        var packages = configReader.GetPackages(true);

                        packages?.ToList().ForEach(package => {
                            var reference = new ExternalReference() { Identity = package.PackageIdentity.Id, Version = package.PackageIdentity.Version.OriginalVersion };
                            if (!_externalReferences.NugetReferences.Contains(reference))
                            {
                                _externalReferences.NugetReferences.Add(reference);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while parsing file {0}", packageConfig);
                }
            }
        }

        private void LoadFromCompilation(HashSet<string> projectReferenceNames)
        {
            if(_compilation != null)
            {
                var externalReferencesMetaData = _compilation.ExternalReferences;

                foreach (var externalReferenceMetaData in externalReferencesMetaData)
                {
                    try
                    {
                        var symbol = _compilation.GetAssemblyOrModuleSymbol(externalReferenceMetaData) as IAssemblySymbol;

                        var filePath = externalReferenceMetaData.Display;

                        //We were able to find a nupkg file and load the package, we don't need to do manual processing
                        //if (LoadFromNugetFile(filePath)) continue;

                        var name = Path.GetFileNameWithoutExtension(externalReferenceMetaData.Display);
                        var externalReference = new ExternalReference()
                        {
                            AssemblyLocation = filePath
                        };

                        if (symbol == null)
                        {
                            var assemblyName = AssemblyName.GetAssemblyName(externalReferenceMetaData.Display);
                            externalReference.Identity = assemblyName?.Name;
                            externalReference.Version = assemblyName?.Version?.ToString();
                            name = assemblyName?.Name;
                        }
                        else if (symbol != null && symbol.Identity != null)
                        {
                            externalReference.Identity = symbol.Identity.Name;
                            externalReference.Version = symbol.Identity.Version != null ? symbol.Identity.Version.ToString() : string.Empty;
                            name = symbol.Identity.Name;
                        }

                        var nugetRef = _externalReferences.NugetReferences.FirstOrDefault(n => n.Identity == name);

                        if (nugetRef == null)
                        {
                            nugetRef = _externalReferences.NugetReferences.FirstOrDefault(n => filePath.ToLower().Contains(string.Concat(Constants.PackagesDirectoryIdentifier, n.Identity.ToLower(), ".", n.Version)));
                        }

                        if (nugetRef != null)
                        {
                            //Nuget with more than one dll?
                            if (string.IsNullOrEmpty(nugetRef.AssemblyLocation))
                            {
                                nugetRef.AssemblyLocation = filePath;
                            }
                            else
                            {
                                _externalReferences.NugetDependencies.Add(new ExternalReference { Identity = Path.GetFileNameWithoutExtension(filePath), Version = externalReference.Version, AssemblyLocation = filePath });
                            }

                            //If version isn't resolved, get from external reference
                            if (string.IsNullOrEmpty(nugetRef.Version) || !Regex.IsMatch(nugetRef.Version, @"([0-9])+(\.)([0-9])+(\.)([0-9])+"))
                            {
                                nugetRef.Version = externalReference.Version;
                            }
                        }
                        else if (filePath.IndexOf(Common.Constants.PackagesDirectoryIdentifier, System.StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            _externalReferences.NugetDependencies.Add(externalReference);
                        }
                        else if (!projectReferenceNames.Any(n => n.StartsWith(name)))
                        {
                            _externalReferences.SdkReferences.Add(externalReference);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error while resolving reference {0}", externalReferenceMetaData);
                    }
                }
            }
        }
        private bool LoadFromNugetFile(string filePath)
        {
            var foundPackage = false;
            var packageDirSplit = filePath.Split(new string[] { Constants.PackagesDirectoryIdentifier }, StringSplitOptions.RemoveEmptyEntries);
            
            if(packageDirSplit.Length > 1)
            {
                var packageFolderName = packageDirSplit[1].Split(Path.DirectorySeparatorChar)[0];
                var packageFolderDir = Path.Combine(packageDirSplit[0], Constants.PackagesFolder, packageFolderName);
                var nupkg = Directory.EnumerateFiles(packageFolderDir, Constants.NupkgFileExtension, SearchOption.AllDirectories).FirstOrDefault();
                
                var reader = new PackageArchiveReader(nupkg);
                var package = reader.GetIdentity();
                if (package != null)
                {
                    foundPackage = true;

                    var reference = new ExternalReference() { AssemblyLocation = filePath, Identity = package.Id, Version = package.Version.OriginalVersion };
                    var nugetExists = _externalReferences.NugetReferences.FirstOrDefault(f => f.Identity == reference.Identity);

                    if (nugetExists == null)
                    {
                        _externalReferences.NugetReferences.Add(reference);
                    }
                    else if (!reference.Equals(nugetExists))
                    {
                        //Nuget was added, but doesn't have an assembly location
                        nugetExists.AssemblyLocation = reference.AssemblyLocation;
                    }
                }
            }
            return foundPackage;
        }
    }
}
