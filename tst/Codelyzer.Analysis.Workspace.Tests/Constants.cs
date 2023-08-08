namespace Codelyzer.Analysis.Workspace.Tests
{
    internal static class Constants
    {
        // Do not change these values without updating the corresponding line in .gitignore:
        //  **/Projects/Temp
        //  **/Projects/Downloads
        // This is to prevent test projects from being picked up in git after failed unit tests.
        internal static readonly string[] TempProjectDirectories = { "Projects", "Temp" };
        internal static readonly string[] TempProjectDownloadDirectories = { "Projects", "Downloads" };
    }
}