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

        public static Process SetupNotebookProcessRun(string notebookPath, string? outputNotebookPath = null)
        {
            Process process = new Process();
            process.StartInfo.FileName = "dotnet-repl";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments = $"--run {notebookPath} --exit-after-run";

            // Optionally include the output notebook path in case we care about the output.
            if (!string.IsNullOrEmpty(outputNotebookPath))
            {
                process.StartInfo.Arguments += $" --output-path {outputNotebookPath}";
            }

            // The working directory should be the directory that the notebook is in.
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(notebookPath);
            return process;
        }

        public static void RunNotebookThatsExpectedToPass(string notebookPath) 
        {
            string tempPathForOutputNotebook = Path.GetTempFileName();
            using (Process dotnetReplProcess = SetupNotebookProcessRun(notebookPath, tempPathForOutputNotebook))
            {
                dotnetReplProcess.Start();
                dotnetReplProcess.WaitForExit(TIMEOUT);

                // If notebook execution fails, parse the notebook and find the error.
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

        public static void RunNotebookThatsExpectedToFail(string notebookPath)
        {
            string tempPathForOutputNotebook = Path.GetTempFileName();
            using (Process dotnetReplProcess = SetupNotebookProcessRun(notebookPath, tempPathForOutputNotebook))
            {
                dotnetReplProcess.Start();
                dotnetReplProcess.WaitForExit(TIMEOUT);
                dotnetReplProcess.ExitCode.Should().NotBe(0);
            }
        }

        public static NotebookRoot? ParseNotebook(string notebookPath)
        {
            string failedNotebookText = File.ReadAllText(notebookPath);
            NotebookRoot? deserializedNotebook = JsonConvert.DeserializeObject<NotebookRoot>(failedNotebookText);
            deserializedNotebook.Should().NotBeNull();
            return deserializedNotebook;
        }

        public static void ParseNotebookAndFindErrors(string outputNotebookPath, string baseNotebookPath)
        {
            NotebookRoot? deserializedNotebook = ParseNotebook(outputNotebookPath);
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
