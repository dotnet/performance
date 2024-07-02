using System.Diagnostics;
using System.Net;

namespace GC.Infrastructure.Core.TraceCollection
{
    public enum CollectType
    {
        none,
        gc,
        verbose,
        cpu,
        cpu_managed,
        threadtime,
        threadtime_managed,
        join
    }

    public sealed class TraceCollector : IDisposable
    {
        private bool disposedValue;

        private static readonly Lazy<WebClient> _client = new();

        // TODO: Make this URL configurable.
        private const string PERFVIEW_URL = "https://github.com/microsoft/perfview/releases/download/v3.0.0/PerfView.exe";

        private readonly string ALWAYS_ARGS = @$" /AcceptEULA /NoGUI /Merge:true";
        internal static readonly Dictionary<CollectType, string> WindowsCollectTypeMap = new()
        {
            { CollectType.gc, "/GCCollectOnly" },
            { CollectType.verbose, "/ClrEventLevel:Verbose /ClrEvents:GC+Stack" },
            { CollectType.cpu,  "/KernelEvents=Process+Thread+ImageLoad+Profile /ClrEventLevel:Informational /ClrEvents:GC+Stack /BufferSize:3000 /CircularMB:3000"  },
            { CollectType.cpu_managed,  "/KernelEvents=Process+Thread+ImageLoad+Profile /ClrEventLevel:Informational /ClrEvents:GC+Stack+Codesymbols+JitSymbols+Compilation+Type+GCHeapAndTypeNames /BufferSize:3000 /CircularMB:3000"  },
            { CollectType.threadtime_managed, "/KernelEvents=Process+Thread+ImageLoad+Profile+ContextSwitch+Dispatcher /ClrEvents:GC+Stack+Codesymbols+JitSymbols+Compilation+Type+GCHeapAndTypeNames /BufferSize:3000 /CircularMB:3000 /ClrEventLevel=Verbose" },
            { CollectType.threadtime, "/KernelEvents=Process+Thread+ImageLoad+Profile+ContextSwitch+Dispatcher /ClrEvents:GC /ClrEventLevel=Verbose /BufferSize:3000 /CircularMB:3000 " },
            { CollectType.join, " /BufferSizeMB:4096 /CircularMB:4096 /KernelEvents:Process+Thread+ImageLoad  /ClrEvents:GC+Threading /ClrEventLevel=Verbose " },
        };

        internal static readonly Dictionary<CollectType, string> LinuxCollectTypeMap = new()
        {
            { CollectType.gc, "gcCollectOnly" },
            { CollectType.cpu,  "collect_cpu" },
            { CollectType.threadtime, "collect_threadTime" },
        };

        internal static readonly Dictionary<string, CollectType> StringToCollectTypeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "gc", CollectType.gc },
            { "verbose", CollectType.verbose },
            { "cpu", CollectType.cpu },
            { "cpu_managed", CollectType.cpu_managed },
            { "threadtime", CollectType.threadtime },
            { "threadtime_managed", CollectType.threadtime_managed },
            { "join", CollectType.join },
            { "none", CollectType.none }
        };

        private readonly string arguments;
        private readonly Guid _sessionName;
        private readonly Process _traceProcess;
        private readonly CollectType _collectType;

        public TraceCollector(string name, string collectType, string outputPath)
        {
            // Get Perfview if it doesn't exist.
            if (!Directory.Exists("./dependencies"))
            {
                Directory.CreateDirectory("./dependencies");
            }

            if (!File.Exists(Path.Combine("./dependencies", "PerfView.exe")))
            {
                _client.Value.DownloadFile(PERFVIEW_URL, Path.Combine("./dependencies", "PerfView.exe"));
            }

            _collectType = StringToCollectTypeMap[collectType];

            if (_collectType != CollectType.none)
            {
                _sessionName = Guid.NewGuid();
                foreach (var invalid in Path.GetInvalidPathChars())
                {
                    name = name.Replace(invalid.ToString(), string.Empty);
                }

                name = name.Replace("<", "");
                name = name.Replace(">", "");

                Name = Path.Combine(outputPath, $"{name}.etl");

                arguments = $"{ALWAYS_ARGS} /sessionName:{_sessionName} {WindowsCollectTypeMap[_collectType]} /LogFile:{Path.Combine(outputPath, name)}.txt /DataFile:{Path.Combine(outputPath, $"{name}.etl")}";
                string command = $"start {arguments}";

                _traceProcess = new();
                _traceProcess.StartInfo.FileName = "./dependencies/PerfView.exe";
                _traceProcess.StartInfo.Arguments = command;
                _traceProcess.StartInfo.UseShellExecute = false;
                _traceProcess.StartInfo.CreateNoWindow = true;
                _traceProcess.StartInfo.RedirectStandardError = true;
                _traceProcess.StartInfo.RedirectStandardOutput = true;
                _traceProcess.Start();

                // Give PerfView about a second to get started.
                Thread.Sleep(1000);
            }
        }

        private void Dispose(bool disposing)
        {
            if (_collectType == CollectType.none)
            {
                disposedValue = true;
                return;
            }

            // TODO: Parameterize the wait for exit time.

            if (!disposedValue)
            {
                using (Process stopProcess = new())
                {
                    stopProcess.StartInfo.FileName = Path.Combine("./dependencies", "PerfView.exe");
                    string command = $"stop {arguments}";
                    stopProcess.StartInfo.Arguments = command;
                    stopProcess.StartInfo.UseShellExecute = false;
                    stopProcess.StartInfo.CreateNoWindow = true;
                    stopProcess.StartInfo.RedirectStandardInput = true;
                    stopProcess.StartInfo.RedirectStandardError = true;
                    stopProcess.Start();
                    stopProcess.WaitForExit(200_000);
                    _traceProcess?.Dispose();
                }

                // Clean up any dangling ETW sessions for both the Kernel and the session.
                using (Process stopLogmanKernelProcess = new())
                {
                    stopLogmanKernelProcess.StartInfo.FileName = "logman";
                    string etsStopCommand = $"-ets stop {_sessionName}Kernel";
                    stopLogmanKernelProcess.StartInfo.Arguments = etsStopCommand;
                    stopLogmanKernelProcess.StartInfo.UseShellExecute = false;
                    stopLogmanKernelProcess.StartInfo.RedirectStandardOutput = false;
                    stopLogmanKernelProcess.StartInfo.RedirectStandardError = false;
                    stopLogmanKernelProcess.StartInfo.CreateNoWindow = true;
                    stopLogmanKernelProcess.Start();
                    stopLogmanKernelProcess.WaitForExit(5_000);
                }

                using (Process stopLogmanProcess = new())
                {
                    stopLogmanProcess.StartInfo.FileName = "logman";
                    string etsStopCommand = $"-ets stop {_sessionName}";
                    stopLogmanProcess.StartInfo.Arguments = etsStopCommand;
                    stopLogmanProcess.StartInfo.UseShellExecute = false;
                    stopLogmanProcess.StartInfo.RedirectStandardOutput = false;
                    stopLogmanProcess.StartInfo.RedirectStandardError = false;
                    stopLogmanProcess.StartInfo.CreateNoWindow = true;
                    stopLogmanProcess.Start();
                    stopLogmanProcess.WaitForExit(5_000);
                }

                disposedValue = true;
            }
        }

        public string Name { get; init; }

        ~TraceCollector()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
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