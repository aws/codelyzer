using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Codelyzer.Analysis.Common
{
    public static class FileUtils
    {
        public static void WriteFile(string path, string data)
        {
            System.IO.File.WriteAllText(path, data);
        }
        
        public static async Task<string> WriteFileAsync(string dir, string file, string content)
        {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(dir, file)) )
            {
                await outputFile.WriteAsync(content);
            }

            return Path.Combine(dir, file);
        }
        
        public static string ReadFile(string pathFile)
        {
            return File.ReadAllText(pathFile);
        }
        
        public static string GetRelativePath(string filePath, string dirPath)
        {
            var dirPathSeparator = Path.EndsInDirectorySeparator(dirPath) ? dirPath : 
                Path.Combine(dirPath, Path.DirectorySeparatorChar.ToString());
            
            var path = filePath.Replace(dirPathSeparator, "");
            return path;
        }

        public static void DirectoryCopy(string sourceDirPath, string destDirPath, bool copySubDirs = true)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirPath);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirPath);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirPath);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirPath, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirPath, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        public static IEnumerable<string> GetProjectPathsFromSolutionFile(string solutionPath)
        {
            if (solutionPath.Contains(".sln") && File.Exists(solutionPath))
            {
                string solutionDir = Directory.GetParent(solutionPath).FullName;
                IEnumerable<string> projectPaths = null;
                try
                {
                    SolutionFile solution = SolutionFile.Parse(solutionPath);
                    projectPaths = solution.ProjectsInOrder.Select(p => p.AbsolutePath);
                }
                catch (Exception ex)
                {
                    //Should include the logger here
                    //LogHelper.LogError(ex, $"Error while parsing solution file {solutionPath} falling back to directory parsing.");
                    projectPaths = Directory.EnumerateFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);
                }

                return projectPaths;
            }
            return new List<string>();
        }
    }
}
