using Microsoft.Diagnostics.Utilities;
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

        private static readonly string DependenciesFolder = "./dependencies";
        
        public string Name { get; init; }

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

        internal static readonly Dictionary<CollectType, string> LinuxServerRunCollectTypeMap = new()
        {
            { CollectType.gc, "gcCollectOnly" },
            { CollectType.cpu,  "collect_cpu" },
            { CollectType.threadtime, "collect_threadTime" }
        };

        internal static readonly Dictionary<CollectType, string> LinuxLocalRunCollectTypeMap = new()
        {
            { CollectType.gc, "--profile gc-collect" },
            { CollectType.cpu, "" },
            { CollectType.verbose, "--clrevents gc+stack --clreventlevel verbose" }
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

        private readonly CollectType _collectType;
        private readonly Process _traceProcess;
        private readonly string arguments;
        private readonly string _collectorPath;
#if Windows
        private readonly Guid _sessionName;
#endif

        private static void InstallTraceCollector(string dependenciesFolder)
        {
#if Windows
            string perfviewPath = Path.Combine(DependenciesFolder, "Perfview.exe");
            if (File.Exists(perfviewPath))
            {
                return;
            }
            
            // TODO: Make this URL configurable.
            const string perfviewUrl = "https://github.com/microsoft/perfview/releases/download/v3.0.0/PerfView.exe";
            using (HttpClient client = new())
            {
                HttpResponseMessage response = client.GetAsync(perfviewUrl).Result;
                response.EnsureSuccessStatusCode();

                using (FileStream writer = File.OpenWrite(perfviewPath))
                {
                    response.Content.ReadAsStream().CopyTo(writer);
                }
            }
#else
            string dotNetTracePath = Path.Combine(DependenciesFolder, "dotnet-trace");
            if (File.Exists(dotNetTracePath))
            {
                return;
            }

            using (Process dotNetTraceInstaller = new())
            {
                dotNetTraceInstaller.StartInfo.FileName = "dotnet";
                dotNetTraceInstaller.StartInfo.Arguments = $"tool install dotnet-trace --tool-path {dependenciesFolder}";
                dotNetTraceInstaller.StartInfo.UseShellExecute = false;
                dotNetTraceInstaller.StartInfo.CreateNoWindow = true;
                dotNetTraceInstaller.StartInfo.RedirectStandardError = true;
                dotNetTraceInstaller.StartInfo.RedirectStandardOutput = true;
                dotNetTraceInstaller.Start();
                dotNetTraceInstaller.WaitForExit();
            }
#endif
        }

        public TraceCollector(string name, string collectType, string outputPath, int? pid = null)
        {
            if (!Directory.Exists(DependenciesFolder))
            {
                Directory.CreateDirectory(DependenciesFolder);
            }

            InstallTraceCollector(DependenciesFolder);

            _collectType = StringToCollectTypeMap[collectType];

            if (_collectType == CollectType.none)
            {
                return;
            }

            foreach (var invalid in Path.GetInvalidPathChars())
            {
                name = name.Replace(invalid.ToString(), string.Empty);
            }

            name = name.Replace("<", "");
            name = name.Replace(">", "");

#if Windows
            _collectorPath = Path.Combine(DependenciesFolder, "Perfview.exe");
            _sessionName = Guid.NewGuid();

            Name = Path.Combine(outputPath, $"{name}.etl");
            string ALWAYS_ARGS = @$" /AcceptEULA /NoGUI /Merge:true";
            arguments = $"{ALWAYS_ARGS} /sessionName:{_sessionName} {WindowsCollectTypeMap[_collectType]} /LogFile:{Path.Combine(outputPath, name)}.txt /DataFile:{Name}";
            string command = $"start {arguments}";

            _traceProcess = new();
            _traceProcess.StartInfo.FileName = _collectorPath;
            _traceProcess.StartInfo.Arguments = command;
            _traceProcess.StartInfo.UseShellExecute = false;
            _traceProcess.StartInfo.CreateNoWindow = true;
            _traceProcess.StartInfo.RedirectStandardError = true;
            _traceProcess.StartInfo.RedirectStandardOutput = true;
            _traceProcess.Start();

            // Give PerfView about a second to get started.
            Thread.Sleep(1000);

#else
            if (pid == null)
            {
                throw new Exception($"{nameof(TraceCollector)}: Must provide prcoess id in Linux case");
            }

            if (_collectType != CollectType.none && !LinuxLocalRunCollectTypeMap.Keys.Contains(_collectType))
            {
                throw new Exception($"{nameof(TraceCollector)}: Trace collect type {collectType} is not supported for Linux local run.");
            }

            _collectorPath = Path.Combine(DependenciesFolder, "dotnet-trace");

            Name = Path.Combine(outputPath, $"{name}.nettrace");
            arguments = $"-p {pid} -o {Name} {LinuxLocalRunCollectTypeMap[_collectType]}";
            string command = $"collect {arguments}";

            _traceProcess = new();
            _traceProcess.StartInfo.FileName = _collectorPath;
            _traceProcess.StartInfo.Arguments = command;
            _traceProcess.StartInfo.UseShellExecute = false;
            _traceProcess.StartInfo.CreateNoWindow = true;
            _traceProcess.StartInfo.RedirectStandardError = true;
            _traceProcess.StartInfo.RedirectStandardOutput = true;
            _traceProcess.Start();
#endif
        }

        private void Dispose(bool disposing)
        {
            if (_collectType == CollectType.none)
            {
                disposedValue = true;
                return;
            }

            // TODO: Parameterize the wait for exit time.

#if Windows
            if (!disposedValue)
            {
                using (Process stopProcess = new())
                {
                    stopProcess.StartInfo.FileName = _collectorPath;
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
#else
            if (!disposedValue)
            {
                _traceProcess.WaitForExit();
                _traceProcess.Dispose();
            }
#endif
        }


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