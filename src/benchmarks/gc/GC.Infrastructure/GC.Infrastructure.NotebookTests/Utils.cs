using FluentAssertions;
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

        public static Process SetupNotebookProcessRun(string notebookFile, string? outputPath = "")
        {
            Process process = new Process();
            process.StartInfo.FileName = "dotnet-repl";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments = $"--run {notebookFile} --exit-after-run";
            if (!string.IsNullOrEmpty(outputPath))
            {
                process.StartInfo.Arguments += $" --output-path {outputPath}";
            }

            return process;
        }

        public static void RunNotebookThatsExpectedToPass(string notebookPath)
        {
            string tempPathForOutputNotebook = Path.GetTempFileName();
            using (Process dotnetReplProcess = SetupNotebookProcessRun(notebookPath, tempPathForOutputNotebook))
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

        public static void RunNotebookThatsExpectedToFail(string notebookPath)
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
    }
}
