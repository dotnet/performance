using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;

namespace ScenarioMeasurement;

public class LinuxTraceSession : ITraceSession
{
    public string TraceFilePath
    {
        get { return perfCollect?.TraceFilePath; }
    }
    private readonly PerfCollect perfCollect;
    private readonly Action<string, string> environmentVariableSetter;
    private Dictionary<TraceSessionManager.KernelKeyword, PerfCollect.KernelKeyword> kernelKeywords;
    private Dictionary<TraceSessionManager.ClrKeyword, PerfCollect.ClrKeyword> clrKeywords;

    public LinuxTraceSession(string sessionName, string traceName, string traceDirectory, Logger logger, Action<string, string> environmentVariableSetter)
    {
        perfCollect = new PerfCollect(traceName, traceDirectory, logger);
        this.environmentVariableSetter = environmentVariableSetter;
        InitLinuxKeywordMaps();
    }

    public void EnableProviders(IParser parser)
    {
        // Enable both providers and start the session
        parser.EnableKernelProvider(this);
        parser.EnableUserProviders(this);
        perfCollect.Start();
    }

    public void Dispose()
    {
        perfCollect.Stop();
    }

    public void EnableKernelProvider(params TraceSessionManager.KernelKeyword[] keywords)
    {
        foreach (var keyword in keywords)
        {
            perfCollect.AddKernelKeyword(kernelKeywords[keyword]);
        }
    }

    public void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords)
    {
        foreach (var keyword in keywords)
        {
            perfCollect.AddClrKeyword(clrKeywords[keyword]);
        }
    }

    public void AddRawEvent(string name)
    {
        perfCollect.AddRawEvent(name);
    }

    private void InitLinuxKeywordMaps()
    {
        // initialize linux kernel keyword map
        kernelKeywords = new Dictionary<TraceSessionManager.KernelKeyword, PerfCollect.KernelKeyword>();
        kernelKeywords[TraceSessionManager.KernelKeyword.Process] = PerfCollect.KernelKeyword.LTTng_Kernel_ProcessLifetimeKeyword;
        kernelKeywords[TraceSessionManager.KernelKeyword.Thread] = PerfCollect.KernelKeyword.LTTng_Kernel_ThreadKeyword;
        kernelKeywords[TraceSessionManager.KernelKeyword.ContextSwitch] = PerfCollect.KernelKeyword.LTTng_Kernel_ContextSwitchKeyword;

        // initialize linux clr keyword map
        clrKeywords = new Dictionary<TraceSessionManager.ClrKeyword, PerfCollect.ClrKeyword>();
        clrKeywords[TraceSessionManager.ClrKeyword.Startup] = PerfCollect.ClrKeyword.DotNETRuntimePrivate_StartupKeyword;
    }

    public void EnableUserProvider(string provider, TraceEventLevel verboseLevel)
    {
        // Enable all EventSource events on Linux
        perfCollect.AddClrKeyword(PerfCollect.ClrKeyword.EventSource);
        // Filter events from the provider
        environmentVariableSetter?.Invoke("COMPlus_EventSourceFilter", provider);
    }
}
