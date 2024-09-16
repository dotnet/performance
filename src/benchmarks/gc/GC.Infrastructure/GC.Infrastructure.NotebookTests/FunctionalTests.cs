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

        [Test]
        public void FunctionalTest_RunAllNotebooksToCheckForOutputs_NoOutputsExpected()
        {
            string notebookPath = Utils.GetNotebookDirectoryPath();
            List<string> notebooksWithOutputs = new();
            // .dib files don't have any output to check.
            // We don't want to enumerate what the notebooks will be before hand because this test should be 
            // more stringent and should be done for all cases.
            Directory.EnumerateFiles(notebookPath, "*.ipynb", SearchOption.AllDirectories)
                .ToList()
                .ForEach(notebook =>
                {
                    bool outputsDetected = Utils.CheckIfNotebookHasOutputs(notebook);
                    if (outputsDetected)
                    {
                        notebooksWithOutputs.Add(Path.GetFileName(notebook));
                    }
                });

            if (notebooksWithOutputs.Count > 0)
            {
                throw new NotebookOutputDetectionException(notebooksWithOutputs);
            }
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