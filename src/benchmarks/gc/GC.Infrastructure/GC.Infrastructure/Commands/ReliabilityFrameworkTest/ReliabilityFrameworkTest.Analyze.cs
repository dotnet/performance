
using GC.Infrastructure.Core.Configurations.ReliabilityFrameworkTest;

namespace GC.Infrastructure.Commands.ReliabilityFrameworkTest
{
    public static class ReliabilityFrameworkTestDumpAnalyzer
    {
        public static void AnalyzeDumps(ReliabilityFrameworkTestAnalyzeConfiguration configuration)
        {
            foreach (string dumpPath in Directory.GetFiles(configuration.DumpFolder, "*.dmp"))
            {
                Console.WriteLine($"====== Debugging {dumpPath} ======");

                string dumpName = Path.GetFileNameWithoutExtension(dumpPath);
                string callStackOutputPath = Path.Combine(
                    configuration.AnalyzeOutputFolder, $"{dumpName}_callstack.txt");
                string callStackForAllThreadsOutputPath = Path.Combine(
                    configuration.AnalyzeOutputFolder, $"{dumpName}_callstack_allthreads.txt");
                List<string> debugCommandList = new List<string>(){
                    $".sympath {configuration.CoreRoot}",
                    ".reload",
                    $".logopen {callStackOutputPath}",
                    "k",
                    ".logclose",
                    $".logopen {callStackForAllThreadsOutputPath}",
                    "~*k",
                    ".logclose",
                };

                string debuggerScriptPath = Path.Combine(configuration.AnalyzeOutputFolder, "debugging-script.txt");
                DebuggingInvoker.GenerateDebuggingScript(debuggerScriptPath, debugCommandList);
                DebuggingInvoker.DebugDump(new Dictionary<string, string>(), "", dumpPath, debuggerScriptPath);
            }
        }
    }
}
