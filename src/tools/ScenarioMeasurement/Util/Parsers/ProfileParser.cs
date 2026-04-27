using Microsoft.Diagnostics.Tracing.Session;
using ScenarioMeasurement;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.Tracing.Parsers;
using Reporting;

namespace ScenarioMeasurement;

public class ProfileParser : IParser
{
    readonly IParser other;
    public ProfileParser(IParser other) => this.other = other;

    public void EnableKernelProvider(ITraceSession kernel)
    {
        var kernelSession = ((WindowsTraceSession)kernel).KernelSession;
        kernelSession.StackCompression = true;
        var keywords = KernelTraceEventParser.Keywords.Default;
        kernelSession.EnableKernelProvider(keywords, keywords);
    }

    public void EnableUserProviders(ITraceSession user)
    {
        other.EnableUserProviders(user);
        var userSession = ((WindowsTraceSession)user).UserSession;
        // make sure we turn on whatever the user wanted so the start/stops are findable.
        userSession.EnableProvider(ClrTraceEventParser.ProviderGuid, Microsoft.Diagnostics.Tracing.TraceEventLevel.Verbose, (ulong)ClrTraceEventParser.Keywords.Default);
        userSession.EnableProvider(ClrPrivateTraceEventParser.ProviderGuid, Microsoft.Diagnostics.Tracing.TraceEventLevel.Verbose, (ulong)(ClrPrivateTraceEventParser.Keywords.GC
            | ClrPrivateTraceEventParser.Keywords.Binding
            | ClrPrivateTraceEventParser.Keywords.Fusion
            | ClrPrivateTraceEventParser.Keywords.MulticoreJit
            | ClrPrivateTraceEventParser.Keywords.Startup
            | ClrPrivateTraceEventParser.Keywords.Stack));

        // Microsoft-Windows-Kernel-File, for stacks on file creates etc.
        var stacksEnabled = new TraceEventProviderOptions { StacksEnabled = true };
        userSession.EnableProvider(new Guid("EDD08927-9CC4-4E65-B970-C2560FB5C289"), Microsoft.Diagnostics.Tracing.TraceEventLevel.Verbose, 0x80, stacksEnabled);

        //.NET Tasks
        var options = new TraceEventProviderOptions { StacksEnabled = true };
        if (TraceEventProviderOptions.FilteringSupported)
        {
            options.EventIDStacksToEnable = new List<int>() { 7, 10, 12 };
        }
        userSession.EnableProvider(TplEtwProviderTraceEventParser.ProviderGuid, Microsoft.Diagnostics.Tracing.TraceEventLevel.Verbose, (ulong)TplEtwProviderTraceEventParser.Keywords.Default, options);

        // .NETFramework
        userSession.EnableProvider(FrameworkEventSourceTraceEventParser.ProviderGuid, Microsoft.Diagnostics.Tracing.TraceEventLevel.Verbose, (ulong)(
                               FrameworkEventSourceTraceEventParser.Keywords.ThreadPool |
                               FrameworkEventSourceTraceEventParser.Keywords.ThreadTransfer |
                               FrameworkEventSourceTraceEventParser.Keywords.NetClient),
                               stacksEnabled);
    }

    public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
    {
        throw new NotImplementedException("Parsing is not supported on ProfileParser");
    }
}
