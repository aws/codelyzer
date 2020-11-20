using Codelyzer.Analysis.Common;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

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
        public bool MethodInvocations;
        public bool LiteralExpressions;
        public bool LambdaMethods;
        public bool DeclarationNodes;
        public bool Annotations;
        public bool LocationData = true;
        public bool ReferenceData;
        public bool LoadBuildData = false;
        public bool InterfaceDeclarations = false;
        public bool GenerateBinFiles = false;
    }
}
