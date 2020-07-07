using System;
using System.Collections.Generic;

namespace AwsCodeAnalyzer
{
    public class AnalyzerOptions
    {
        public const string LANGUAGE_CSHARP = "CSharp";

        public string Language;
        public string JsonOutputPath;

        private readonly ISet<string> options;
        public AnalyzerOptions(string language)
        {
            Language = language;
            options = new HashSet<string>();
        }

        public void AddOption(string name)
        {
            options.Add(name);
        }
    }
}