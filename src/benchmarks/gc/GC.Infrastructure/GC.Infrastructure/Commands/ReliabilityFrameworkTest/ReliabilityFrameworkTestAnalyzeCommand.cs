using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.ReliabilityFrameworkTest;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;



namespace GC.Infrastructure.Commands.ReliabilityFrameworkTest
{
    public sealed class ReliabilityFrameworkTestAnalyzeCommand :
        Command<ReliabilityFrameworkTestAnalyzeCommand.ReliabilityFrameworkTestAnalyzeSettings>
    {
        public class CommandInvokeResult
        {
            public int ExitCode { get; set; }
            public string StdOut { get; set; }
            public string StdErr { get; set; }
        }

        public sealed class ReliabilityFrameworkTestAnalyzeSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public required string ConfigurationPath { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, 
                                    [NotNull] ReliabilityFrameworkTestAnalyzeSettings settings)
        {
            AnsiConsole.Write(new Rule("Analyze Dumps Collected In Reliability Framework Test"));
            AnsiConsole.WriteLine();

            ConfigurationChecker.VerifyFile(settings.ConfigurationPath,
                                            nameof(ReliabilityFrameworkTestAnalyzeSettings));
            ReliabilityFrameworkTestAnalyzeConfiguration configuration =
                ReliabilityFrameworkTestAnalyzeConfigurationParser.Parse(settings.ConfigurationPath);

            Directory.CreateDirectory(configuration.AnalyzeOutputFolder);

            AnalyzeDumps(configuration);

            return 0;
        }

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
                GenerateDebuggingScript(debuggerScriptPath, debugCommandList);
                DebugDump(new Dictionary<string, string>(), "", dumpPath, debuggerScriptPath);
            }
        }
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
            var process = new Process();
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            try
            {
                process.StartInfo.FileName = OperatingSystem.IsWindows() ? "windbgx" : "lldb";
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.UseShellExecute = false;

                process.StartInfo.Arguments = OperatingSystem.IsWindows()
                    ? $"-c $<{debuggerScriptPath} -z {dumpPath}"
                    : $"-c {dumpPath} -s {debuggerScriptPath} --batch";

                foreach (var kvp in env)
                {
                    process.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }

                process.StartInfo.RedirectStandardOutput = redirectStdOutErr;
                process.StartInfo.RedirectStandardError = redirectStdOutErr;

                if (redirectStdOutErr)
                {
                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            stdout.AppendLine(args.Data);
                            if (!silent) Console.WriteLine($"STDOUT: {args.Data}");
                        }
                    };

                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            stderr.AppendLine(args.Data);
                            if (!silent) Console.WriteLine($"STDERR: {args.Data}");
                        }
                    };
                }

                process.Start();

                if (redirectStdOutErr)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                process.WaitForExit();

                return new CommandInvokeResult
                {
                    ExitCode = process.ExitCode,
                    StdOut = stdout.ToString(),
                    StdErr = stderr.ToString()
                };
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
