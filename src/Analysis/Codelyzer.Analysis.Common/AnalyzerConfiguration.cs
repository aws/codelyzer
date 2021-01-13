using Codelyzer.Analysis.Common;

namespace Codelyzer.Analysis
{
    public class AnalyzerConfiguration
    {
        public string Language;
        public int ConcurrentThreads = Constants.DefaultConcurrentThreads;

        public AnalyzerConfiguration(string language)
        {
            Language = language;
            ExportSettings = new ExportSettings(); 
            MetaDataSettings = new MetaDataSettings();
        }

        public ExportSettings ExportSettings;
        public MetaDataSettings MetaDataSettings;
    }
    
    public static class LanguageOptions
    {
        public const string CSharp = nameof(CSharp);
        public const string Vb = nameof(Vb);
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
        public bool MethodInvocations { get; set; }
        public bool LiteralExpressions { get; set; }
        public bool LambdaMethods { get; set; }
        public bool DeclarationNodes { get; set; }
        public bool Annotations { get; set; }
        public bool LocationData { get; set; } = true;
        public bool ReferenceData { get; set; }
        public bool LoadBuildData { get; set; } = false;
        public bool InterfaceDeclarations { get; set; } = false;
        public bool EnumDeclarations { get; set; } = false;
        public bool StructDeclarations { get; set; } = false;
        public bool ReturnStatements { get; set; } = false;
        public bool InvocationArguments { get; set; } = false;
        public bool GenerateBinFiles { get; set; } = false;
    }
}
