using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace ScenarioMeasurement;

public class WindowsTraceSession : ITraceSession
{
    private readonly Logger logger;
    public string TraceFilePath { get; }
    public TraceEventSession KernelSession { get; set; }
    public TraceEventSession UserSession { get; set; }
    private Dictionary<TraceSessionManager.KernelKeyword, KernelTraceEventParser.Keywords> kernelKeywords;
    private Dictionary<TraceSessionManager.ClrKeyword, ClrPrivateTraceEventParser.Keywords> clrKeywords;

    public WindowsTraceSession(string sessionName, string traceName, string traceDirectory, Logger logger)
    {
        this.logger = logger;
        if (!IsAdministrator())
        {
            Console.WriteLine("Admin mode is required to start ETW.");
            Environment.Exit(1);
        }
        var kernelFileName = Path.ChangeExtension(traceName, "perflabkernel.etl");
        var userFileName = Path.ChangeExtension(traceName, "perflabuser.etl");
        TraceFilePath = Path.Combine(traceDirectory, Path.ChangeExtension(traceName, ".etl"));

        KernelSession = new TraceEventSession(sessionName + "_kernel", Path.Combine(traceDirectory, kernelFileName));
        UserSession = new TraceEventSession(sessionName + "_user", Path.Combine(traceDirectory, userFileName));
        InitWindowsKeywordMaps();
    }

    public void EnableProviders(IParser parser)
    {
        // Enable both providers and start the session
        parser.EnableKernelProvider(this);
        parser.EnableUserProviders(this);
    }

    public void Dispose()
    {
        KernelSession.Dispose();
        UserSession.Dispose();

        MergeFiles(KernelSession.FileName, UserSession.FileName, TraceFilePath);

        logger.Log($"Trace Saved to {TraceFilePath}");
    }

    private void MergeFiles(string kernelTraceFile, string userTraceFile, string traceFile)
    {
        var files = new List<string>();
        if (!File.Exists(kernelTraceFile))
        {
            Console.WriteLine("Kernel trace file not found.");
            Environment.Exit(1);
        }
        files.Add(kernelTraceFile);
        if (File.Exists(userTraceFile))
        {
            files.Add(userTraceFile);
        }

        logger.Log($"Merging {string.Join(',', files)}... ");
        try
        {
            TraceEventSession.Merge(files.ToArray(), traceFile);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            Console.WriteLine("Unable to merge the trace files due to insufficient system resources. Try freeing some memory.");
            Environment.Exit(1);
        }
        if (File.Exists(traceFile))
        {
            File.Delete(userTraceFile);
            File.Delete(kernelTraceFile);
        }
    }

    public void EnableKernelProvider(params TraceSessionManager.KernelKeyword[] keywords)
    {
        // Create keyword flags for windows events
        KernelTraceEventParser.Keywords flags = 0;
        foreach (var keyword in keywords)
        {
            flags |= kernelKeywords[keyword];
        }
        var enabled = false;
        try
        {
            enabled = KernelSession.EnableKernelProvider(flags);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
        }
        finally
        {
            if (!enabled)
            {
                Console.WriteLine("Unable to enable kernel provider. Try freeing some memory.");
                Environment.Exit(1);
            }
        }

    }

    public void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords)
    {
        // Create keyword flags for windows events
        ClrPrivateTraceEventParser.Keywords flags = 0;
        foreach (var keyword in keywords)
        {
            flags |= clrKeywords[keyword];
        }
        var enabled = false;
        try
        {
            enabled = UserSession.EnableProvider(ClrPrivateTraceEventParser.ProviderGuid, TraceEventLevel.Verbose, (ulong)flags);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
        }
        finally{
            if (!enabled)
            {
/*                    Console.WriteLine("Unable to enable user provider due to insufficient system resources. Try freeing some memory.");
                Environment.Exit(1);*/
            }
        }
    }

    public void AddRawEvent(string name)
    {
        throw new NotSupportedException("RawEvents are supported only for PerfCollect.");
    }

    private void InitWindowsKeywordMaps()
    {
        // initialize windows kernel keyword map
        kernelKeywords = new Dictionary<TraceSessionManager.KernelKeyword, KernelTraceEventParser.Keywords>();
        kernelKeywords[TraceSessionManager.KernelKeyword.Process] = KernelTraceEventParser.Keywords.Process;
        kernelKeywords[TraceSessionManager.KernelKeyword.Thread] = KernelTraceEventParser.Keywords.Thread;
        kernelKeywords[TraceSessionManager.KernelKeyword.ContextSwitch] = KernelTraceEventParser.Keywords.ContextSwitch;

        // initialize windows clr keyword map
        clrKeywords = new Dictionary<TraceSessionManager.ClrKeyword, ClrPrivateTraceEventParser.Keywords>();
        clrKeywords[TraceSessionManager.ClrKeyword.Startup] = ClrPrivateTraceEventParser.Keywords.Startup;
    }

    public void EnableUserProvider(string provider, TraceEventLevel verboseLevel)
    {
        UserSession.EnableProvider(provider, verboseLevel);
    }

    public static bool IsAdministrator()
    {
#pragma warning disable CA1416 // WindowsTraceSession only called from TraceSessionManager if platform is windows
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
#pragma warning restore CA1416
    }
}
