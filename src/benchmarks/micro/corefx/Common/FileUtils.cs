using System.IO;

namespace System.IO
{
    public static class FileUtils
    {
        public static string GetTestFilePath() 
            => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    }
}