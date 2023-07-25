

using Codelyzer.Analysis.Model;

namespace Codelyzer.Analysis.Workspace.Tests
{
    public static class ExpectedResults
    {
        public static Dictionary<string, object> GetExpectedAnalyzerResults(string solutionName)
        {
            return solutionName switch
            {
                "OwinParadise.sln" => GetOwinParadiseResults(),
                "CoreWebApi.sln" => GetCoreWebApiResults(),
                _ => throw new Exception("Test results for solution name not found")
            };
        }

        private static Dictionary<string, object> GetOwinParadiseResults()
        {
            return new Dictionary<string, object>()
            {
                { "BlockStatementsCount", 2 },
                { "ClassesCount", 1 },
                { "ExpressionsCount", 19 },
                { "InvocationExpressionsCount", 14 },
                { "LiteralExpressionsCount", 5 },
                { "MethodsCount", 2 },
                { "ReturnStatementsCount", 0 },
                { "AnnotationsCount", 0 },
                { "NamespacesCount", 1 },
                { "ObjectCreationCount", 8 },
                { "UsingDirectivesCount", 10 },
                { "ArgumentsCount", 14 },
                { "MemberAccessExpressionsCount", 6 },
                { "NugetReferencesCount", 31 },
                { "SourceFilesCount", 14 },
                { "MethodSignature", "public PortingParadise.OwinExtraApi.OwinAuthorization(IAuthorizationRequirement)" },
                { "ClassDeclarationIdentifier", "OwinExtraApi" },
                { "ClassDeclarationModifier", "public"}
            };
        }
        private static Dictionary<string, object> GetCoreWebApiResults()
        {
            return new Dictionary<string, object>()
            {
                { "BlockStatementsCount", 2 },
                { "ClassesCount", 1 },
                { "ExpressionsCount", 23 },
                { "InvocationExpressionsCount", 8 },
                { "LiteralExpressionsCount", 15 },
                { "MethodsCount", 1 },
                { "ReturnStatementsCount", 1 },
                { "AnnotationsCount", 3 },
                { "NamespacesCount", 1 },
                { "ObjectCreationCount", 2 },
                { "UsingDirectivesCount", 6 },
                { "ArgumentsCount", 8 },
                { "MemberAccessExpressionsCount", 8 },
                { "NugetReferencesCount", 0 },
                { "SourceFilesCount", 6 },
                { "MethodSignature", "public CoreWebApi.Controllers.WeatherForecastController.Get()" },
                { "ClassDeclarationIdentifier", "WeatherForecastController" },
                { "ClassDeclarationModifier", "public"}
            };
        }
    }
}