using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading;
using Xunit;

namespace Startup.Tests;

public class EvaluationHookLifecycleTests
{
    [WindowsFileLockTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void LifecycleLocksAreBoundedAndPersistentFailuresRemainVisible(bool releaseLock)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var token = Guid.NewGuid().ToString("N");
        var providerName = "PerfTest-Lifecycle-" + token;
        var previousDirectory = Environment.GetEnvironmentVariable("PERFSTAR_EVALUATION_METRICS_DIR");
        var previousSession = Environment.GetEnvironmentVariable("PERFSTAR_EVALUATION_METRICS_SESSION");
        Thread release = null;
        try
        {
            Environment.SetEnvironmentVariable("PERFSTAR_EVALUATION_METRICS_DIR", root);
            Environment.SetEnvironmentVariable("PERFSTAR_EVALUATION_METRICS_SESSION", token);
            StartupHook.InitializeForTests(providerName);
            var lifecycle = Directory.GetFiles(root, "*.lifecycle.tsv")[0];
            var held = new FileStream(lifecycle, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (releaseLock)
            {
                release = new Thread(() =>
                {
                    SpinWait.SpinUntil(() => File.Exists(lifecycle + ".retries.txt"), TimeSpan.FromSeconds(2));
                    held.Dispose();
                });
                release.Start();
            }
            using (var source = new EventSource(providerName))
            {
                release?.Join();
                held.Dispose();
                StartupHook.FlushForTests();
                if (releaseLock)
                {
                    Assert.Empty(Directory.GetFiles(root, "*.error.txt"));
                }
                else
                {
                    Assert.Single(Directory.GetFiles(root, "*.error.txt"));
                    Assert.Equal(5, File.ReadAllLines(lifecycle + ".retries.txt").Length);
                }
                Assert.NotEmpty(Directory.GetFiles(root, "*.retries.txt"));
                var fields = File.ReadAllLines(lifecycle)[1].Split('\t');
                Assert.False(string.IsNullOrEmpty(fields[4]));
                Assert.False(string.IsNullOrEmpty(fields[5]));
            }
        }
        finally
        {
            release?.Join();
            StartupHook.ResetForTests();
            Environment.SetEnvironmentVariable("PERFSTAR_EVALUATION_METRICS_DIR", previousDirectory);
            Environment.SetEnvironmentVariable("PERFSTAR_EVALUATION_METRICS_SESSION", previousSession);
            Directory.Delete(root, recursive: true);
        }
    }

    public sealed class WindowsFileLockTheory : TheoryAttribute
    {
        public WindowsFileLockTheory()
        {
            if (!OperatingSystem.IsWindows())
            {
                Skip = "Requires Windows file-sharing semantics.";
            }
        }
    }
}
