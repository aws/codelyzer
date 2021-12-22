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

        public static void CreateDirectory(string path)
        {
            System.IO.Directory.CreateDirectory(path);
        }
        
        public static string GetRelativePath(string filePath, string dirPath)
        {
            var dirPathSeparator = Path.EndsInDirectorySeparator(dirPath) ? dirPath : 
                Path.Combine(dirPath, Path.DirectorySeparatorChar.ToString());
            
            var path = filePath.Replace(dirPathSeparator, "");
            return path;
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
