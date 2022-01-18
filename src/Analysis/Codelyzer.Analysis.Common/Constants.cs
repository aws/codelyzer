using Microsoft.Build.Construction;
using System.Collections.Generic;
using System.IO;

namespace Codelyzer.Analysis.Common
{
    public class Constants
    {
        private static string _packagesDirectoryIdentifier;

        public const int DefaultConcurrentThreads = 4;
        public const string MsBuildCommandName = "msbuild";
        public const string RestorePackagesConfigArgument = "/p:RestorePackagesConfig=true";
        public const string LanguageVersionArgument = "/p:langversion=latest";
        public const string EnableNuGetPackageRestore = "EnableNuGetPackageRestore";
            
        public const string ProjectReferenceType = "Microsoft.CodeAnalysis.CSharp.CSharpCompilationReference";

        public const string TargetFrameworks = "TargetFrameworks";
        public const string Version = "Version";
        public const string PackagesFolder = "packages";
        public const string NupkgFileExtension = "*.nupkg";
        public const string PackagesConfig = "packages.config";

        public static HashSet<SolutionProjectType> AcceptedProjectTypes = new HashSet<SolutionProjectType>()
        {
            SolutionProjectType.KnownToBeMSBuildFormat,
            SolutionProjectType.WebDeploymentProject,
            SolutionProjectType.WebProject
        };

        public static string PackagesDirectoryIdentifier
        {
            get
            {
                if (string.IsNullOrEmpty(_packagesDirectoryIdentifier))
                {
                    _packagesDirectoryIdentifier = string.Concat(Path.DirectorySeparatorChar, PackagesFolder, Path.DirectorySeparatorChar);
                }
                return _packagesDirectoryIdentifier;
            }
        }
    }
}
