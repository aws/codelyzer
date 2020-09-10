using System;
using System.Collections.Generic;
using System.Text;

namespace AwsCodeAnalyzer.Common
{
    public class Constants
    {
        public const string MsBuildCommandName = "msbuild";
        public const string RestorePackagesConfigArgument = "/p:RestorePackagesConfig=true";

        public const string ProjectReferenceType = "Microsoft.CodeAnalysis.CSharp.CSharpCompilationReference";
        public const string MsCorlib = "mscorlib";
    }
}
