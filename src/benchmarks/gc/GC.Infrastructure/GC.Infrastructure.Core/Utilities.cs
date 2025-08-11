namespace GC.Infrastructure.Core
{
    public static class Utilities
    {
        public static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            sourcePath = Path.GetFullPath(sourcePath).TrimEnd(Path.DirectorySeparatorChar);
            targetPath = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar);

            Directory.CreateDirectory(targetPath);

            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourcePath, dirPath);
                string newDirPath = Path.Combine(targetPath, relativePath);
                Directory.CreateDirectory(newDirPath);
            }

            foreach (string filePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourcePath, filePath);
                string newFilePath = Path.Combine(targetPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                File.Copy(filePath, newFilePath, true);
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

        public static void CopyFile(string srcPath, string dstPath)
        {
            string realDestPath = String.Empty;
            if (Directory.Exists(dstPath))
            {
                // Copy file to a directory
                string fileName = Path.GetFileName(srcPath);
                realDestPath = Path.Combine(dstPath, fileName);
            }
            else
            {
                realDestPath = dstPath;
            }

            File.Copy(srcPath, realDestPath);
        }
    }
}
