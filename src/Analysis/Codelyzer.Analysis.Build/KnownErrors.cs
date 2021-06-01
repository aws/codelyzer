using System;
using System.Collections.Generic;
using System.Text;

namespace Codelyzer.Analysis.Build
{
    public class KnownErrors
    {
        public const string MsBuildMissing = "Couldn't find a .NET Framework MSBuild path";
        public const string NoMainMethodMessage = "Program does not contain a static 'Main' method suitable for an entry point";
    }
}
