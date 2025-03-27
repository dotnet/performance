namespace GC.Infrastructure.Core.Functionality
{
    public static class UtilitiesCommon
    {
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
    }
}
