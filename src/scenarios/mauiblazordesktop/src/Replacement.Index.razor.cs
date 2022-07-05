using Microsoft.AspNetCore.Components;
using System.Diagnostics.Tracing;
namespace MauiBlazorDesktopTesting.Pages
{
    public partial class Index
    {
        private static EventSource log = new EventSource(
            "Perf-Custom-Event",
            EventSourceSettings.EtwSelfDescribingEventFormat);
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                log.Write("FirstRender", new EventSourceOptions {Level=EventLevel.Verbose, Opcode=EventOpcode.Info });
            }
        }
    }
}