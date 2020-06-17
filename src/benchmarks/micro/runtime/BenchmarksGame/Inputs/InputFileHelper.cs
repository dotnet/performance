using System;
using System.IO;

namespace BenchmarksGame
{
    internal static class InputFileHelper
    {
        internal static string FindInputFile(string inputFile)
        {
            if (string.IsNullOrEmpty(inputFile))
                throw new ArgumentNullException(nameof(inputFile));

            var fullPath = GetFullPath(inputFile);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Unable to find input file.", inputFile);

            return fullPath;
        }

        internal static int GetFileLength(string filePath) => (int) new FileInfo(filePath).Length;

        private static string GetFullPath(string fileName)
            => Path.Combine(Path.GetDirectoryName(typeof(InputFileHelper).Assembly.Location), "runtime", "BenchmarksGame", "Inputs", fileName);
    }
}