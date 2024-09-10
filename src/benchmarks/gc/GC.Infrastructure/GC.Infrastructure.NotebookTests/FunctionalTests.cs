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
        }

        [Test]
        public void FunctionalTest_RunGCAnalysisExamples_Success()
        {
            // Conjecture: All examples should be functional.
            string notebookPath = Path.Combine(Utils.GetNotebookDirectoryPath(), "Examples", "GCAnalysisExamples.ipynb");
            Utils.RunNotebookThatsExpectedToPass(notebookPath);
        }

        [Test]
        public void FunctionalTest_RunBenchmarkAnalysis_Success()
        {
            // TODO: Parameterize the notebook paths.
            string notebookPath = Path.Combine(Utils.GetNotebookDirectoryPath(), "BenchmarkAnalysis.dib");
            Utils.RunNotebookThatsExpectedToPass(notebookPath);
        }

        [Test]
        public void FunctionalTest_IntentionallyFailedNotebook_Failure()
        {
            string? executionPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.IsNotNull(executionPath);
            string failureNotebookPath = Path.Combine(executionPath, "TestNotebooks");
            string[] failedNotebookPaths = Directory.GetFiles(failureNotebookPath);

            foreach (var failedNotebookPath in failedNotebookPaths)
            {
                Utils.RunNotebookThatsExpectedToFail(failedNotebookPath);
            }
        }
    }
}