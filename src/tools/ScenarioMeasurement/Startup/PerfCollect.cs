using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace ScenarioMeasurement;

public class PerfCollect : IDisposable
{
    private readonly string startupDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private ManagedProcessHelper perfCollectProcess;
    public string TraceName { get; private set; }
    public string TraceFileName { get; private set; }
    public string TraceDirectory { get; private set; }
    public string TraceFilePath { get; private set; }
    private List<KernelKeyword> KernelEvents = new List<KernelKeyword>();
    private List<ClrKeyword> ClrEvents = new List<ClrKeyword>();
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

        if (String.IsNullOrEmpty(traceName))
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
            throw new Exception("Lttng installation failed. Please try manual install.");
        }
    }

    public Result Start()
    {
        var arguments = new StringBuilder($"start {TraceName} -events ");

        foreach (var keyword in KernelEvents)
        {
            arguments.Append(keyword.ToString());
            arguments.Append(",");
        }

        foreach (var keyword in ClrEvents)
        {
            arguments.Append(keyword.ToString());
            arguments.Append(",");
        }

        var args = arguments.Remove(arguments.Length - 1, 1).ToString();

        perfCollectProcess.Arguments = args;
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
        if (LttngInstalled())
        {
            Console.WriteLine("Lttng is already installed.");
            return Result.Success;
        }
        perfCollectProcess.Arguments = "install -force";
        perfCollectProcess.Run();

        var retry = 10;
        for(var i=0; i<retry; i++)
        {
            if (!LttngInstalled())
            {
                Console.WriteLine($"Lttng not installed. Retry {i}...");
                perfCollectProcess.Run();
            }
            else
            {
                return Result.Success;
            }
        }
        return Result.CloseFailed;
    }

    public void Dispose()
    {
        Stop();
    }

    public void AddClrKeyword(ClrKeyword keyword)
    {
        ClrEvents.Add(keyword);
    }

    public void AddKernelKeyword(KernelKeyword keyword)
    {
        KernelEvents.Add(keyword);
    }

    private bool LttngInstalled()
    {
        var procStartInfo = new ProcessStartInfo("modinfo", "lttng_probe_writeback");
        var proc = new Process() { StartInfo = procStartInfo, };
        proc.StartInfo.RedirectStandardOutput = true;
        proc.Start();
        var result = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        // If the lttng_probe_writeback module is installed, the modinfo output will include the filename field
        return result.Contains("filename:");
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
