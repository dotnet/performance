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
        }

        [Test]
        public void FunctionalTest_GCAnalysisExamples_Success()
        {
            string notebookPath = "GCAnalysisExamples.ipynb";
            string examplesPath = Path.Combine(Utils.GetNotebookDirectoryPath(), "Examples");
            Utils.RunNotebookThatsExpectedToPass(notebookPath, examplesPath);
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
            string failureNotebookPath = Path.Combine(executionPath, "TestNotebooks");
            string[] failedNotebookPaths = Directory.GetFiles(failureNotebookPath);

            foreach (var failedNotebookPath in failedNotebookPaths)
            {
                Utils.RunNotebookThatsExpectedToFail(failedNotebookPath, failureNotebookPath);
            }
        }
    }
}