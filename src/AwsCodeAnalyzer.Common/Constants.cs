using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AwsCodeAnalyzer.Common
{
    public class Constants
    {
        private static string _packagesDirectoryIdentifier;

        public const string MsBuildCommandName = "msbuild";
        public const string RestorePackagesConfigArgument = "/p:RestorePackagesConfig=true";

        public const string ProjectReferenceType = "Microsoft.CodeAnalysis.CSharp.CSharpCompilationReference";

        public const string TargetFrameworks = "TargetFrameworks";

        public static string PackagesDirectoryIdentifier
        {
            get
            {
                if (string.IsNullOrEmpty(_packagesDirectoryIdentifier))
                {
                    _packagesDirectoryIdentifier = string.Concat(Path.DirectorySeparatorChar, "packages", Path.DirectorySeparatorChar);
                }
                return _packagesDirectoryIdentifier;
            }
        }
    }
}
