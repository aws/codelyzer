![Build Test Publish](https://github.com/aws/codename-codelyzer/workflows/Build%20Test%20Publish/badge.svg?branch=mainline&event=push)

## AwsCodeAnalyzer
The AwsCodeAnalyzer is  a framework that provides interfaces to build and analyze the source code in various languages, and generates the platform-independent representation of artifacts – UAST or Code Graph or both. It offers fine-grained controls on what kind of metadata – properties of classes, methods, etc., - and how deep in the hierarchy of the source code to gather information while generating the artifacts.

Code Analyzer creates an instance of Analyzer engine based on language and other settings. For example, for C# it creates a RoslynProcessor.  Similarly, for Java it creates a JavaParserProcessor. Then, it creates an asynchronous task to analyze the source code.

## AwsCodeAnalyzerRoslyn
AwsCodeAnalyzerRoslyn is an analyzer engine for languages like CSharp and VB, based on Roslyn compiler platform.
The RoslynProcessor walks the AST to collect metadata of source file components – solution, projects, namespaces, classes, methods, method invocations, and literal expressions, etc. It uses symbol table to replace properties with full qualified names. 

Code Analyzer invokes Graph Handler to build Code Graph based on UAST model.

### How to use this package?

* Load the solution `AwsCodeAnalyzer.soln` using Visual studio or Rider. 
* Create a new class with Main in it.

* Create AnalyzerOptions object and fill the settings as needed. Set the output path for Json files.
* Get CodeAnalyzer instance from factory class.
* Either call AnalyzeProject or AnalyzeSolution.

```csharp
Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

AnalyzerOptions options = new AnalyzerOptions(AnalyzerOptions.LANGUAGE_CSHARP);
            options.JsonOutputPath = @"/Users/shiramsn/encore/analysis";
            
CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(options, Log.Logger);

var result = await analyzer.AnalyzeProject(path);
```

#### Sample Json output
```json
{
  "version": "1.0",
  "generated-by": "Auto generated by the Codelyzer  on: Monday, 06 July 2020",
  "workspace-name": "AmazonSourceCodeAnalyzerRoslyn",
  "workspace-root-path": "/Users/shiramsn/workplace/encore/src/AmazonSourceCodeAnalyzerRoslyn",
  "source-files": [
    "CallHierarchyContext.cs",
    "CSharpCoreAnalyzer.cs",
    "CSharpCoreWalker.cs",
  ],
  "errors-found": 0,
  "source-file-results": [
    {
      "type": {
        "id": 100,
        "name": "source-file-root"
      },
      "language": "C#",
      "file-path": "CallHierarchyContext.cs",
      "file-full-path": "/Users/shiramsn/workplace/encore/src/AmazonSourceCodeAnalyzerRoslyn/CallHierarchyContext.cs",
      "children": [
        {
          "type": {
            "id": 102,
            "name": "using-dir-or-import"
          },
          "identifier": "System",
          "location": {
            "start-char-position": 1,
            "end-char-position": 14,
            "start-line-position": 1,
            "end-line-position": 1
          },
          "children": []
        },
        
        {
          "type": {
            "id": 103,
            "name": "namespace-or-package"
          },
          "identifier": "Amazon.CodeAnalysis.Analyzer.Roslyn",
          "location": {
            "start-char-position": 1,
            "end-char-position": 2,
            "start-line-position": 6,
            "end-line-position": 86
          },
          "children": [
            {
              "type": {
                "id": 104,
                "name": "class"
              },
              "identifier": "CallHierarchyContext",
              "location": {
                "start-char-position": 5,
                "end-char-position": 6,
                "start-line-position": 8,
                "end-line-position": 85
              },
              "children": [
                {
                  "type": {
                    "id": 105,
                    "name": "method"
                  },
                  "identifier": "enterNamespace",
                  "location": {
                    "start-char-position": 9,
                    "end-char-position": 10,
                    "start-line-position": 23,
                    "end-line-position": 36
                  },
                  "modifiers": "public",
                  "parameters": [
                    {
                      "name": "encoreNamespaceNode",
                      "type": "NamespaceModel",
                      "semantic-type": "NamespaceModel"
                    }
                  ],
                  "return-type": "void",
                  "semantic-return-type": "Void",
                  "semantic-properties": [
                    "Public"
                  ],
                  "children": [
                    {
                      "type": {
                        "id": 106,
                        "name": "body"
                      },
                      "identifier": "block",
                      "location": {
                        "start-char-position": 9,
                        "end-char-position": 10,
                        "start-line-position": 24,
                        "end-line-position": 36
                      },
                      "children": [
                        {
                          "type": {
                            "id": 108,
                            "name": "invocation"
                          },
                          "identifier": "namespaceStack.Peek().AddChild(encoreNamespaceNode)",
                          "location": {
                            "start-char-position": 17,
                            "end-char-position": 68,
                            "start-line-position": 28,
                            "end-line-position": 28
                          },
                          "method-name": "AddChild",
                          "semantic-namespace-or-package": "Amazon.CodeAnalysis.Model",
                          "caller-identifier": "namespaceStack.Peek()",
                          "semantic-method-signature": "Amazon.CodeAnalysis.Model.BaseObjectModel.AddChild(Amazon.CodeAnalysis.Model.BaseObjectModel)",
                          "semantic-return-type": "Void",
                          "semantic-original-def": "Amazon.CodeAnalysis.Model.BaseObjectModel.AddChild(Amazon.CodeAnalysis.Model.BaseObjectModel)",
                          "semantic-properties": [
                            "Public",
                            "abstract"
                          ],
                          "children": []
                        },
                        {
                          "type": {
                            "id": 108,
                            "name": "invocation"
                          },
                          "identifier": "namespaceStack.Peek()",
                          "location": {
                            "start-char-position": 17,
                            "end-char-position": 38,
                            "start-line-position": 28,
                            "end-line-position": 28
                          },
                          "method-name": "Peek",
                          "semantic-namespace-or-package": "System.Collections.Generic",
                          "caller-identifier": "namespaceStack",
                          "semantic-method-signature": "System.Collections.Generic.Stack<Amazon.CodeAnalysis.Model.NamespaceModel>.Peek()",
                          "semantic-return-type": "NamespaceModel",
                          "semantic-original-def": "System.Collections.Generic.Stack<T>.Peek()",
                          "semantic-properties": [
                            "Public"
                          ],
                          "children": []
                        }
                    }
                }
            }
        }
    }

```

