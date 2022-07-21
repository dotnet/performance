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
            //using (StreamWriter sw = File.AppendText(@".\traces\testblazorhybridfile.txt"))
            //{
            //    sw.WriteLine("This is the new text");
            //}
            //System.Console.WriteLine("First Render!!!!");
            log.Write("FirstRender", new EventSourceOptions {Level=EventLevel.LogAlways, Opcode=EventOpcode.Info });
            //Environment.Exit(97);
        }
    }
}