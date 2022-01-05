namespace Codelyzer.Analysis.Tests
{
    internal class Constants
    {
        // Do not change these values without updating the corresponding line in .gitignore:
        //  **/Projects/Temp
        //  **/Projects/Downloads
        // This is to prevent test projects from being picked up in git after failed unit tests.
        internal static readonly string[] TempProjectDirectories = { "Projects", "Temp" };
        internal static readonly string[] TempProjectDownloadDirectories = { "Projects", "Downloads" };

        internal static string programFiles = "Program Files";
        internal static string programFilesx86 = "Program Files (x86)";
        internal static string vs2022MSBuildPath = @"Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe";
        internal static string vs2019MSBuildPath = @"Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe";
        internal static string vs2017MSBuildPath = @"Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe";
        internal static string vs2019BuildToolsMSBuildPath = @"Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe";
        internal static string MSBuild14Path = @"Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe";
    }
}