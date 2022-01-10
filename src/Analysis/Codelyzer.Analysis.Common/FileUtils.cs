using System.IO;
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
    }
}
