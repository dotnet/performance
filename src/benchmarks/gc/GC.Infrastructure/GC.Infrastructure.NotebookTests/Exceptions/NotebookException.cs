namespace GC.Infrastructure.NotebookTests.Exceptions
{
    public sealed class NotebookExecutionException : Exception
    {
        public NotebookExecutionException(string exceptionMessage, string notebookName)
            : base($"{notebookName} failed with: {exceptionMessage}") { }
    }
}
