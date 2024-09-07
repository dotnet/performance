using FluentAssertions;
using System.Reflection;
using GC.Infrastructure.NotebookTests.NotebookParser;
using Newtonsoft.Json;
using System.Diagnostics;

namespace GC.Infrastructure.NotebookTests
{
    internal static class Utils
    {
        internal const int TIMEOUT = 120_000;
        public static bool CheckDotnetReplInstalled()
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "dotnet-repl";
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;

                try
                {
                    process.Start();
                    process.WaitForExit();
                }

                catch (System.ComponentModel.Win32Exception)
                {
                    return false;
                }

                return process.ExitCode == 0;
            }
        }

        public static void InstallDotnetRepl()
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = "tool install --local dotnet-repl";
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();
            }
        }

        public static Process SetupNotebookProcessRun(string notebookFile, string? outputPath = "", string? overrideWorkingDirectory = "")
        {
            Process process = new Process();
            process.StartInfo.FileName = "dotnet-repl";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments = $"--run {notebookFile} --exit-after-run";
            if (!string.IsNullOrEmpty(outputPath))
            {
                process.StartInfo.Arguments += $" --output-path {outputPath}";
            }
            
            // Working Directory for the process should be that of the "Notebooks" directory by default.
            if (!string.IsNullOrEmpty(overrideWorkingDirectory))
            {
                process.StartInfo.WorkingDirectory = overrideWorkingDirectory;
            }

            else
            {
                process.StartInfo.WorkingDirectory = GetNotebookDirectoryPath();
            }

            return process;
        }

        public static void RunNotebookThatsExpectedToPass(string notebookPath, string overrideRunDirectory = "")
        {
            string tempPathForOutputNotebook = Path.GetTempFileName();
            using (Process dotnetReplProcess = SetupNotebookProcessRun(notebookPath, tempPathForOutputNotebook, overrideRunDirectory))
            {
                dotnetReplProcess.Start();
                dotnetReplProcess.WaitForExit(TIMEOUT);

                if (dotnetReplProcess.ExitCode != 0)
                {
                    ParseNotebookAndFindErrors(tempPathForOutputNotebook, notebookPath);
                }

                else
                {
                    dotnetReplProcess.HasExited.Should().BeTrue();
                }
            }

            // Cleanup any temp notebooks.
            File.Delete(tempPathForOutputNotebook);
        }

        public static void RunNotebookThatsExpectedToFail(string notebookPath, string overrideRunDirectory = "")
        {
            using (Process dotnetReplProcess = SetupNotebookProcessRun(notebookPath))
            {
                dotnetReplProcess.Start();
                dotnetReplProcess.WaitForExit(TIMEOUT);
                dotnetReplProcess.ExitCode.Should().NotBe(0);
            }
        }

        public static void ParseNotebookAndFindErrors(string outputNotebookPath, string baseNotebookPath)
        {
            string failedNotebookText = File.ReadAllText(outputNotebookPath);
            NotebookRoot? deserializedNotebook = JsonConvert.DeserializeObject<NotebookRoot>(failedNotebookText);
            deserializedNotebook.Should().NotBeNull();
            File.Delete(outputNotebookPath);

            foreach (var cell in deserializedNotebook!.Cells)
            {
                foreach (var output in cell.Outputs)
                {
                    if (string.CompareOrdinal(output.Ename, "Error") == 0)
                    {
                        throw new NotebookExecutionException(output.Evalue, baseNotebookPath);
                    }
                }
            }
        }

        public static string GetNotebookDirectoryPath()
        {
            string executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
            string? executionPath = Path.GetDirectoryName(executingAssemblyPath!);
            DirectoryInfo rootDirectoryInfo = Directory.GetParent(executionPath!)?.Parent?.Parent?.Parent?.Parent!;
            string notebookPath = Path.Combine(rootDirectoryInfo!.FullName, "src", "benchmarks", "gc", "GC.Infrastructure", "Notebooks");
            return notebookPath;
        }
    }
}
