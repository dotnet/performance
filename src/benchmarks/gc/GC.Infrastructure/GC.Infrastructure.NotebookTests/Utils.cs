using System.Diagnostics;

namespace GC.Infrastructure.NotebookTests
{
    internal static class Utils
    {
        public static bool CheckDotnetReplInstalled()
        {
            return true;
        }

        public static void InstallDotnetRepl()
        {
        }

        public static Process SetupNotebookProcessRun(string notebookFile, bool saveOutput = false)
        {
            Process process = new Process();
            process.StartInfo.FileName = "dotnet-repl";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments = $"--run {notebookFile} --exit-after-run";
            if (saveOutput)
            {
                // TODO: Add a temporary file to save the output to.
            }

            return process;
        }
    }
}
