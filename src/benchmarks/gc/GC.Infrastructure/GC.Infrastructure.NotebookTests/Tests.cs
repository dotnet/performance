using System.Diagnostics;

namespace GC.Infrastructure.NotebookTests
{
    public class Tests
    {
        private const int TIMEOUT = 60_000;

        [SetUp]
        public void Setup()
        {
            if (!Utils.CheckDotnetReplInstalled())
            {
                Utils.InstallDotnetRepl();
            }

            // Ensure dotnet-repl is installed.
            string currentDir = Directory.GetCurrentDirectory();
            DirectoryInfo? rootDirectoryInfo = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent?.Parent;
            Assert.IsNotNull(rootDirectoryInfo);
            string notebookPath = Path.Combine(rootDirectoryInfo.FullName, "src", "benchmarks", "gc", "GC.Infrastructure", "Notebooks");
            Directory.SetCurrentDirectory(notebookPath);
        }

        [Test]
        public void Test_BenchmarkAnalysis_Success()
        {
            using (Process dotnetReplProcess = Utils.SetupNotebookProcessRun("BenchmarkAnalysis.dib", saveOutput: false))
            {
                dotnetReplProcess.Start();
                dotnetReplProcess.WaitForExit(TIMEOUT);
                Assert.IsTrue(dotnetReplProcess.HasExited);
                Assert.IsTrue(dotnetReplProcess.ExitCode == 0);

                // If exit code != 0, parse out the output and extract the exception.
                //int exitCode = dotnetReplProcess.ExitCode;
                //if (exitCode != 0)
            }
        }
    }
}