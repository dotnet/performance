namespace GC.Infrastructure.NotebookTests.Exceptions
{
    public sealed class NotebookOutputDetectionException : Exception
    {
        public NotebookOutputDetectionException(List<string> notebooks)
            : base($"The following notebooks contained outputs that should be cleared before checking in the notebooks: {string.Join(", ", notebooks)}") 
        {}
    }
}
