using GC.Infrastructure.NotebookTests.NotebookParser;
using Newtonsoft.Json;
using System.Diagnostics;

namespace GC.Infrastructure.NotebookTests
{
    public sealed class NotebookRunner : IDisposable
    {
        internal const int TIMEOUT = 120_000;
        private bool disposedValue;
        private Process? _dotnetReplProcess;
        private readonly string _tempNotebookPath;

        public NotebookRunner(string notebookPath)
        {
            NotebookPath = notebookPath;
            _dotnetReplProcess = new Process();
            _dotnetReplProcess.StartInfo.FileName = "dotnet-repl";
            _dotnetReplProcess.StartInfo.UseShellExecute = false;
            _dotnetReplProcess.StartInfo.Arguments = $"--run {notebookPath} --exit-after-run";
            _tempNotebookPath = Path.GetTempFileName();
            _dotnetReplProcess.StartInfo.Arguments += $" --output-path {_tempNotebookPath}";
            _dotnetReplProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(notebookPath);
            _dotnetReplProcess!.Start();
            _dotnetReplProcess.WaitForExit(TIMEOUT);

            // Assumption: dotnet-repl _always_ creates an output notebook.
            NotebookRoot = JsonConvert.DeserializeObject<NotebookRoot>(File.ReadAllText(_tempNotebookPath));
        }

        public string NotebookPath { get; }
        public NotebookRoot? NotebookRoot { get; private set; }
        public bool? HasExited => _dotnetReplProcess?.HasExited;
        public int? ExitCode => _dotnetReplProcess?.ExitCode;

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    NotebookRoot = null;
                }

                File.Delete(_tempNotebookPath);
                _dotnetReplProcess?.Dispose();
                _dotnetReplProcess = null;
                disposedValue = true;
            }
        }

        ~NotebookRunner()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}
