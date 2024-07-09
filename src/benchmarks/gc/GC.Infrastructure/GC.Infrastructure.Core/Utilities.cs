namespace GC.Infrastructure.Core
{
    public static class Utilities
    {
        public static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            string targetPathAsDirectory = targetPath + "\\";
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPathAsDirectory));
            }

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPathAsDirectory), true);
            }
        }

        public static bool TryCreateDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException($"{nameof(Utilities)}: Path is null or empty.");
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return true;
            }

            else
            {
                return false;
            }
        }

        public static bool TryCopyFile(string sourcePath, string destinationPath)
        {
            if (!File.Exists(destinationPath))
            {
                File.Copy(sourceFileName: sourcePath,
                          destFileName: destinationPath);
                return true;
            }

            else
            {
                return false;
            }
        }
    }
}
