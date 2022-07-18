using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        /// <summary>
        /// This function takes a solution file path .../*.sln and parses the solution file to return all of the project paths contained within.
        /// </summary>
        /// <param name="solutionPath">Path of the solution file *.sln.</param>
        /// <param name="logger">Optional logger object Microsoft.Extension.Logging.ILogger used to log errors during this process.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetProjectPathsFromSolutionFile(string solutionPath, ILogger logger = null)
        {
            if (solutionPath.Contains(".sln") && File.Exists(solutionPath))
            {
                IEnumerable<string> projectPaths = null;
                try
                {
                    SolutionFile solution = SolutionFile.Parse(solutionPath);
                    projectPaths = solution.ProjectsInOrder.Where(p => Constants.AcceptedProjectTypes.Contains(p.ProjectType)).Select(p => p.AbsolutePath);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, $"Error while parsing solution file {solutionPath} falling back to directory parsing.");
                    string solutionDir = Directory.GetParent(solutionPath).FullName;
                    projectPaths = Directory.EnumerateFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);
                }

                return projectPaths;
            }
            else
            {
                logger?.LogError($"Solution file does not exist or is not of .sln format {solutionPath} falling back to directory parsing.");
                string solutionDir = Directory.GetParent(solutionPath).FullName;
                return Directory.EnumerateFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);
            }
        }

        public static Dictionary<string, HashSet<string>> GetProjectsWithReferences(string solutionPath, ILogger logger = null)
        {
            var projectFiles = GetProjectPathsFromSolutionFile(solutionPath);

            Dictionary<string, HashSet<string>> result = new Dictionary<string, HashSet<string>>();
            foreach (var projectFile in projectFiles)
            {
                try
                {
                    var projectDocument = XDocument.Load(projectFile);
                    var projectReferenceNodes = projectDocument
                        .Root
                        .Descendants()
                        .Where(x => x.Name?.LocalName == "ProjectReference")
                        .Select(p => Path.GetFullPath(p.Attribute("Include").Value, Path.GetDirectoryName(projectFile)))
                        .Distinct()
                        .ToHashSet();
                    result.Add(projectFile, projectReferenceNodes);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error while retrieving project references");
                }
            }

            return result;
        }
    }
}
