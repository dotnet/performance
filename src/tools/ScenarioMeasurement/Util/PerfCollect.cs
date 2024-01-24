using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ScenarioMeasurement;

public class PerfCollect : IDisposable
{
    private readonly string startupDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private readonly ManagedProcessHelper perfCollectProcess;
    public string TraceName { get; private set; }
    public string TraceFileName { get; private set; }
    public string TraceDirectory { get; private set; }
    public string TraceFilePath { get; private set; }
    private readonly List<KernelKeyword> KernelEvents = new();
    private readonly List<ClrKeyword> ClrEvents = new();
    private readonly List<string> RawEvents = new();
    public PerfCollect(string traceName, Logger logger) : this(traceName, Environment.CurrentDirectory, logger)
    {
    }

    public PerfCollect(string traceName, string traceDirectory, Logger logger)
    {
        var perfCollectScript = Path.Combine(startupDirectory, "perfcollect");
        if (!File.Exists(perfCollectScript))
        {
            throw new FileNotFoundException($"Pefcollect not found at {perfCollectScript}. Please rebuild the project to download it.");
        }

        if (string.IsNullOrEmpty(traceName))
        {
            throw new ArgumentException("Trace file name cannot be empty.");
        }
        TraceName = traceName.Replace(" ", "_");

        if (!Directory.Exists(traceDirectory))
        {
            Directory.CreateDirectory(traceDirectory);
        }

        TraceDirectory = traceDirectory;
        TraceFileName = $"{TraceName}.trace.zip";
        TraceFilePath = Path.Combine(traceDirectory, TraceFileName);

        perfCollectProcess = new ManagedProcessHelper(logger)
        {
            ProcessWillExit = true,
            Executable = perfCollectScript,
            Timeout = 1200,
            RootAccess = true
        };

        if (Install() != Result.Success)
        {
            throw new Exception("PerfCollect installation failed. Please try manual install.");
        }
    }

    public Result Start()
    {
        var arguments = new StringBuilder();
        arguments.Append($"start {TraceName} -events ");
        var kernelEvents = KernelEvents.Select(x => x.ToString());
        var clrEvents = ClrEvents.Select(x => x.ToString());
        arguments.AppendJoin(',', kernelEvents.Concat(clrEvents));

        if (RawEvents.Any())
        {
            arguments.Append(" -rawevents ");
            arguments.AppendJoin(',', RawEvents);
        }

        perfCollectProcess.Arguments = arguments.ToString();
        return perfCollectProcess.Run().Result;
    }

    public Result Stop()
    {
        var arguments = $"stop {TraceName} ";
        perfCollectProcess.Arguments = arguments;
        var result = perfCollectProcess.Run().Result;
        // By default perfcollect saves traces in the current directory
        if (!File.Exists(TraceFileName))
        {
            throw new FileNotFoundException($"Trace file not found at {Path.GetFullPath(TraceFileName)}.");
        }
        // Don't move file if destination directory is current directory
        if (Path.GetFullPath(Path.GetDirectoryName(TraceFilePath)) != Environment.CurrentDirectory)
        {
            // Overwrite file at destination directory
            if (File.Exists(TraceFilePath))
            {
                Console.WriteLine($"Deleting existing file at {TraceFilePath}...");
                File.Delete(TraceFilePath);
            }
            File.Move(TraceFileName, TraceFilePath);
        }
        //TODO: move logs to appropriate location
        return result;
    }

    public Result Install()
    {
        if (InstallImpl())
        {
            return Result.Success;
        }

        var retry = 10;
        for (var i = 0; i < retry; i++)
        {
            Console.WriteLine($"PerfCollect install retry {i}...");
            if (InstallImpl())
            {
                return Result.Success;
            }
        }

        return Result.ExitedWithError;

        bool InstallImpl()
        {
            if (PerfLabValues.SharedHelpers.IsUbuntu22Queue())
            {
                Console.WriteLine("Installing for Ubuntu 22.");
                InstallUbuntu22Manual();
            }
            perfCollectProcess.Arguments = "install -force";
            return perfCollectProcess.Run().Result == Result.Success;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    public void AddKernelKeyword(KernelKeyword keyword)
    {
        KernelEvents.Add(keyword);
    }

    public void AddClrKeyword(ClrKeyword keyword)
    {
        ClrEvents.Add(keyword);
    }

    public void AddRawEvent(string rawevent)
    {
        RawEvents.Add(rawevent);
    }

    private void InstallUbuntu22Manual()
    {
        var p = new ManagedProcessHelper(perfCollectProcess.Logger)
        {
            ProcessWillExit = true,
            Executable = "apt",
            Arguments = "install -y lttng-tools lttng-modules-dkms liblttng-ust1",
            Timeout = perfCollectProcess.Timeout,
            RootAccess = true,
        };
        p.Run();
    }

    public enum KernelKeyword
    {
        Empty,
        LTTng_Kernel_ProcessLifetimeKeyword,
        LTTng_Kernel_ThreadKeyword,
        LTTng_Kernel_ContextSwitchKeyword
    }

    public enum ClrKeyword
    {
        Empty,
        Threading,
        DotNETRuntimePrivate_StartupKeyword,
        EventSource
    }


}
