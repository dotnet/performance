using System.Runtime.InteropServices;

namespace GC.Infrastructure.Core.Functionality
{
    public static class DotNetInfrastructure
    {
        private static readonly List<string> ValidRIDList = new()
        {
            "win-x64", "win-x86", "win-arm64",
            "linux-x64", "linux-musl-x64",
            "linux-arm64", "linux-musl-arm64",
            "linux-arm", "linux-musl-arm",
            "osx-x64", "osx-arm64",
        };

        public static readonly string CurrentRID = RuntimeInformation.RuntimeIdentifier;

        public static void CheckRID(string targetRID)
        {
            if (!ValidRIDList.Contains(targetRID))
            {
                throw new ArgumentException($"{nameof(DotNetInfrastructure)}: The given RID {targetRID} is invalid");
            }
        }

        public static string GetExcutableFileExtensionByRID(string targetRID)
        {
            CheckRID(targetRID);
            if (targetRID.StartsWith("win"))
            {
                return ".exe";
            }
            else
            {
                return "";
            }
        }
    }
}
