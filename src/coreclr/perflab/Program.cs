using Microsoft.Xunit.Performance.Api;
using System.Reflection;

public class Program
{
    static void Main(string[] args)
    {
        using (XunitPerformanceHarness p = new XunitPerformanceHarness(args))
            p.RunBenchmarks(Assembly.GetEntryAssembly().Location);
    }
}
