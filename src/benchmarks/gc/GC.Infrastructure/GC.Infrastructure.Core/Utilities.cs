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

        public static void CopyFolderRecursively(string sourcePath, string destinationPath)
        {
            string realDestinationPath = String.Empty;
            // If destinationPath is a folder
            if (Directory.Exists(destinationPath))
            {
                string folderName = Path.GetFileName(sourcePath);
                realDestinationPath = Path.Combine(destinationPath, folderName);
            }
            else
            {
                realDestinationPath = destinationPath;
            }
            Directory.CreateDirectory(realDestinationPath);

            foreach (string srcSubDir in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                string dstSubDir = srcSubDir.Replace(sourcePath, realDestinationPath);
                Directory.CreateDirectory(dstSubDir);
            }

            foreach (string srcFile in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                string dstFile = srcFile.Replace(sourcePath, realDestinationPath);
                File.Copy(srcFile, dstFile, true);
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
