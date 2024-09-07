using FluentAssertions;
using System.Reflection;

namespace GC.Infrastructure.NotebookTests
{
    public class FunctionalTests
    {
        [SetUp]
        public void Setup()
        {
            if (!Utils.CheckDotnetReplInstalled())
            {
                Utils.InstallDotnetRepl();
            }

            string currentDir = Directory.GetCurrentDirectory();
            DirectoryInfo? rootDirectoryInfo = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent?.Parent;
            rootDirectoryInfo.Should().NotBeNull();

            // Set the current working directory to that of the Notebooks one.
            // This is done because the notebooks use relative paths to reference other notebooks.
            string notebookPath = Path.Combine(rootDirectoryInfo!.FullName, "src", "benchmarks", "gc", "GC.Infrastructure", "Notebooks");
            Directory.SetCurrentDirectory(notebookPath);
        }

        [Test]
        public void FunctionalTest_GCAnalysisExamples_Success()
        {
            string notebookPath = "GCAnalysisExamples.ipynb";
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Examples"));
            Utils.RunNotebookThatsExpectedToPass(notebookPath);
        }

        [Test]
        public void FunctionalTest_BenchmarkAnalysisFunctionalTest_Success()
        {
            // TODO: Parameterize the notebook paths.
            string notebookPath = "BenchmarkAnalysis.dib";
            Utils.RunNotebookThatsExpectedToPass(notebookPath);
        }

        [Test]
        public void FunctionalTest_IntentionallyFailedNotebook_Failure()
        {
            string? executionPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.IsNotNull(executionPath);
            string failureNotebooks = Path.Combine(executionPath, "TestNotebooks");

            string[] failedNotebookPaths = Directory.GetFiles(failureNotebooks);

            foreach (var failedNotebookPath in failedNotebookPaths)
            {
                Utils.RunNotebookThatsExpectedToFail(failedNotebookPath);
            }
        }
    }
}