# Codelyzer
![Build Test](https://github.com/aws/codename-codelyzer/workflows/Build%20Test/badge.svg)

Codelyzer is a framework that provides interfaces to build and analyze source code in various languages and generates a platform-independent representation as a universal abstract syntax tree (UAST) model or a JSON file. It offers fine-grained controls to specify the kind of metadata (properties of classes, methods, etc.) to gather and how deep in the hierarchy of the code to search while generating these artifacts. Currently, the framework only supports the C# language.

By generating the output as a JSON file, this framework allows you to develop analysis tools in any language.

## Codelyzer - Net

Codelyzer-Net is an analyzer engine for languages based on the Roslyn compiler platform, like C# and VB. The CSharpRoslynProcessor walks an AST to collect metadata of source file components (e.g. solution, projects, namespaces, classes, methods, method invocations, literal expressions, etc). It uses semantic information from a design-time build to collect properties with fully qualified names.

[Codelyzer.Analysis](https://www.nuget.org/packages/Codelyzer.Analysis/) package available to consume from nuget.org.

## Getting Started

Follow the example below to see how the library can be integrated into your application for analyzing .NET application.

```csharp
/* 1. Create logger object */
var loggerFactory = LoggerFactory.Create(builder => 
        builder.SetMinimumLevel(LogLevel.Debug).AddConsole());
var logger = loggerFactory.CreateLogger("Analyzer");
var outputPath = @"/home/users/steve/porting-analysis";

/* 2. Create Configuration settings */
var configuration = new AnalyzerConfiguration(LanguageOptions.CSharp);
configuration.ExportSettings.OutputPath = outputPath;

/* 3. Get Analyzer instance based on language */
var analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, logger);

/* 4. Analyze the project or solution */
var projectFilePath = @"/home/users/steve/projects/TestProject.csproj";
var analyzerResult = await analyzer.AnalyzeProject(projectFilePath);

Console.WriteLine("The results are exported to file : " + analyzerResult.OutputJsonFilePath);

/* 5. Consume the results as model objects */
var sourcefile = analyzerResult.ProjectResult.SourceFileResults.First();
foreach (var invocation in sourcefile.AllInvocationExpressions())
{
    Console.WriteLine(invocation.MethodName + ":" + invocation.SemanticMethodSignature);
}

var objectCreations = sourcefile.AllObjectCreationExpressions();
var allClasses = sourcefile.AllClasses();
var allMethods = sourcefile.AllMethods();
var allLiterals = sourcefile.AllLiterals();
```

## How to use this code?
* Clone the Git repository.
* Load the solution `Codelyzer.sln` using Visual Studio or Rider. 
* Create a "Run/Debug" Configuration for the "Codelyzer.Analysis" project.
* Provide command line arguments for a solution and output path, then run the application.

## Getting Help

Please use these community resources for getting help. We use the GitHub issues
for tracking bugs and feature requests.

* Send us an email to: aws-porting-assistant-support@amazon.com
* If it turns out that you may have found a bug,
  please open an [issue](https://github.com/aws/codelyzer/issues/new)
  
## Contributing

We welcome community contributions and pull requests. See
[CONTRIBUTING](./CONTRIBUTING.md) for information on how to set up a development
environment and submit code.
  
# License

Libraries in this repository are licensed under the Apache 2.0 License.

See [LICENSE](./LICENSE) and [NOTICE](./NOTICE) for more information.    



