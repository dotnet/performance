using FluentAssertions;
using System.Reflection;
using System.Diagnostics;
using GC.Infrastructure.NotebookTests.NotebookParser;
using Newtonsoft.Json;
using GC.Infrastructure.NotebookTests.Exceptions;

namespace GC.Infrastructure.NotebookTests
{
    internal static class Utils
    {
        internal const int TIMEOUT = 240_000;
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

        public static void RunNotebookThatsExpectedToPass(string notebookPath)
        {
            using (NotebookRunner runner = new NotebookRunner(notebookPath))
            {
                if (runner.HasExited.HasValue && !runner.HasExited.Value)
                {
                    // The test timed out => we should throw an exception here.
                    throw new NotebookExecutionException(notebookPath, "Notebook execution timed out. Please try re-running the notebook locally to check for any errors.");
                }

                // Post condition of dotnet-repl is that the output notebook is created and therefore, we are able to deserialize it.
                runner.NotebookRoot.Should().NotBeNull();
                runner.HasExited.Should().BeTrue();

                if (runner.ExitCode.HasValue && runner.ExitCode.Value != 0)
                {
                    ReportErrorInNotebook(runner);
                }
            }
        }

        public static void RunNotebookThatsExpectedToFail(string notebookPath)
        {
            using (NotebookRunner runner = new NotebookRunner(notebookPath))
            {
                runner.ExitCode.Should().NotBe(0);
                bool foundError = false;

                // Post condition of dotnet-repl is that the output notebook is created and therefore,
                // we are able to deserialize it.
                if (runner.ExitCode.HasValue && runner.ExitCode.Value != 0)
                {
                    foreach (var cell in runner.NotebookRoot!.Cells)
                    {
                        foreach (var output in cell.Outputs)
                        {
                            if (output.OutputType == "error")
                            {
                                foundError = true;
                            }
                        }
                    }
                }

                foundError.Should().BeTrue();
            }
        }

        public static void ReportErrorInNotebook(NotebookRunner runner)
        {
            foreach (var cell in runner.NotebookRoot!.Cells)
            {
                foreach (var output in cell.Outputs)
                {
                    if (output.OutputType == "error")
                    {
                        throw new NotebookExecutionException(output.Evalue, runner.NotebookPath);
                    }
                }
            }
        }

        public static bool CheckIfNotebookHasOutputs(string notebookPath)
        {
            NotebookRoot? notebookRoot = JsonConvert.DeserializeObject<NotebookRoot>(File.ReadAllText(notebookPath));
            foreach (var cell in notebookRoot!.Cells)
            {
                if (cell.Outputs.Count > 0)
                {
                    return true;
                }
            }

            return false;
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
