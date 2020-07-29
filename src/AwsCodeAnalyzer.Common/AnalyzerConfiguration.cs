using System;
using System.Collections.Generic;

namespace AwsCodeAnalyzer
{
    public class AnalyzerConfiguration
    {
        public string Language;

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
        public bool GenerateJsonOutput;
        public bool GenerateGremlinOutput;
        public bool GenerateRDFOutput;
        public string OutputPath;
    }
    
    /* By default, it captures Namespaces, directives, classes and methods. */
    public class MetaDataSettings
    {
        public bool MethodInvocations;
        public bool LiteralExpressions;
        public bool LambdaMethods;
    }
}