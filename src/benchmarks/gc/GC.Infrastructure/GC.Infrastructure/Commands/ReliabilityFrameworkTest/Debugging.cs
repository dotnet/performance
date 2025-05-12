namespace GC.Infrastructure.Commands.ReliabilityFrameworkTest
{
    public static class DebuggingInvoker
    {
        public static void GenerateDebuggingScript(string debuggingScriptPath, List<string> DebugCommandList)
        {
            // Generate debug script
            List<string> exitCommandList;
            if (OperatingSystem.IsWindows())
            {
                exitCommandList = new() { ".detach", "q" };
            }
            else
            {
                exitCommandList = new() { "exit" };
            }

            List<string> automationDebuggingCommandList = DebugCommandList
                .Concat(exitCommandList)
                .ToList();

            File.WriteAllLines(debuggingScriptPath, automationDebuggingCommandList);
        }

        public static CommandInvokeResult DebugDump(Dictionary<string, string> env,
                                                    string workingDirectory,
                                                    string dumpPath,
                                                    string debuggerScriptPath,
                                                    bool redirectStdOutErr = true,
                                                    bool silent = true)
        {
            if (OperatingSystem.IsWindows())
            {
                CommandInvoker invoker = new("windbgx",
                                             $"-c $<{debuggerScriptPath} -z {dumpPath}",
                                             env,
                                             workingDirectory,
                                             redirectStdOutErr,
                                             silent);
                return invoker.WaitForResult();
            }
            else 
            {
                CommandInvoker invoker = new("lldb",
                                             $"-c {dumpPath} -s {debuggerScriptPath} --batch",
                                             env,
                                             workingDirectory,
                                             redirectStdOutErr,
                                             silent);
                return invoker.WaitForResult();
            }
        }
    }
}
