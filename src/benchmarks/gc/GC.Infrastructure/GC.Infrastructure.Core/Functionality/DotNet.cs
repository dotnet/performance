using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

        //public static string GetCompressionExtensionByRID(string targetRID)
        //{
        //    CheckRID(targetRID);
        //    if (targetRID.StartsWith("win"))
        //    {
        //        return ".zip";
        //    }
        //    else
        //    {
        //        return ".tar.gz";
        //    }
        //}

        //public static string GetDotNetExecutableFromEnv(Dictionary<string, string> env, string? targetRID = null)
        //{
        //    if (!env.ContainsKey("DOTNET_ROOT"))
        //    {
        //        throw new Exception($"{nameof(DotNetInfrastructure)}: Please set DOTNET_ROOT");
        //    }

        //    string dotNetRoot = env["DOTNET_ROOT"];
        //    if (string.IsNullOrEmpty(targetRID))
        //    {
        //        targetRID = CurrentRID;
        //    }
        //    string exeExtension = GetExcutableFileExtensionByRID(targetRID);
        //    string dotNetExe = Path.Combine(dotNetRoot, $"dotnet{exeExtension}");

        //    return dotNetExe;
        //}

        //public static void ActiveDotNetDumpGeneratingEnvironment(Dictionary<string, string> env,
        //                                                         string dumpPath)
        //{
        //    env["DOTNET_DbgEnableMiniDump"] = "1";
        //    env["DOTNET_DbgMiniDumpType"] = "4";
        //    env["DOTNET_DbgMiniDumpName"] = dumpPath;
        //}
    }
}
