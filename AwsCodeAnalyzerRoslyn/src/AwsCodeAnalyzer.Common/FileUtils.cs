using System;
using System.IO;
using System.Threading.Tasks;

namespace AwsCodeAnalyzer.Common
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
    }
}