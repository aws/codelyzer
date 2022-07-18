using Codelyzer.Analysis.Common;
using System.Collections.Generic;

namespace Codelyzer.Analysis
{
    public class AnalyzerConfiguration
    {
        public string Language;
        public int ConcurrentThreads = Constants.DefaultConcurrentThreads;
        public bool AnalyzeFailedProjects = false; 
        public static List<string> DefaultBuildArguments = new()
        {
            Constants.RestorePackagesConfigArgument,
            Constants.RestoreArgument
        };

        public AnalyzerConfiguration(string language)
        {
            Language = language;
            ExportSettings = new ExportSettings(); 
            MetaDataSettings = new MetaDataSettings();
            BuildSettings = new BuildSettings();
        }

        public ExportSettings ExportSettings;
        public MetaDataSettings MetaDataSettings;
        public BuildSettings BuildSettings;
    }
    
    public static class LanguageOptions
    {
        public const string CSharp = nameof(CSharp);
        public const string Vb = nameof(Vb);
    }

    public class BuildSettings
    {
        public BuildSettings()
        {
            BuildArguments = AnalyzerConfiguration.DefaultBuildArguments;
        }
        public string MSBuildPath;
        public List<string> BuildArguments;
        public bool BuildOnly = false;
        public bool SyntaxOnly = false;
    }

    public class ExportSettings
    {
        public bool GenerateJsonOutput = false;
        public bool GenerateGremlinOutput = false;
        public bool GenerateRDFOutput = false;
        public string OutputPath;
    }
    
    /* By default, it captures Namespaces, directives, classes and methods. */
    public class MetaDataSettings
    {
        public bool MethodInvocations;
        public bool LiteralExpressions;
        public bool LambdaMethods;
        public bool DeclarationNodes;
        public bool Annotations;
        public bool LocationData = true;
        public bool ReferenceData;
        public bool LoadBuildData = false;
        public bool InterfaceDeclarations = false;
        public bool EnumDeclarations = false;
        public bool StructDeclarations = false;
        public bool ReturnStatements = false;
        public bool InvocationArguments = false;
        public bool GenerateBinFiles = false;
        public bool ElementAccess = false;
        public bool MemberAccess = false;
    }
}
