using GC.Infrastructure.NotebookTests.Exceptions;
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

        public static IEnumerable<string> GetAllNotebooks()
        {
            string notebookPath = Utils.GetNotebookDirectoryPath();
            return Directory.EnumerateFiles(notebookPath, "*.ipynb", SearchOption.AllDirectories);
        }

        [Test]
        [TestCaseSource(nameof(GetAllNotebooks))]
        public void FunctionalTest_RunAllNotebooksToCheckForOutputs_NoOutputsExpected(string notebook)
        {
            bool outputDetected = Utils.CheckIfNotebookHasOutputs(notebook);
            Assert.False(outputDetected, $"Notebook {notebook} has outputs. No outputs are expected.");
        }

        [Test]
        [TestCase("GCAnalysisExamples.ipynb")]
        [TestCase("CustomDynamicEvents.ipynb")]
        [TestCase("CPUAnalysisExamples.ipynb")]
        public void FunctionalTest_RunExamples_ExpectsSuccess(string notebookName)
            => Utils.RunNotebookThatsExpectedToPass(Path.Combine(Utils.GetNotebookDirectoryPath(), "Examples", notebookName));

        [Test]
        [TestCase("BenchmarkAnalysis.dib")]
        public void FunctionalTest_RunAnalysisNotebooks_ExpectedSuccess(string notebookName)
            => Utils.RunNotebookThatsExpectedToPass(Path.Combine(Utils.GetNotebookDirectoryPath(), notebookName));

        [Test]
        [TestCase("CompilationFailure.dib")]
        [TestCase("CompilationFailure.ipynb")]
        [TestCase("ExceptionFailure.dib")]
        [TestCase("ExceptionFailure.ipynb")]
        public void FunctionalTest_IntentionallyFailedNotebook_Failure(string notebookName)
        {
            string? executionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string? notebookPath = Path.Combine(executionDirectory!, "TestNotebooks", notebookName);
            Utils.RunNotebookThatsExpectedToFail(notebookPath);
        }
    }
}