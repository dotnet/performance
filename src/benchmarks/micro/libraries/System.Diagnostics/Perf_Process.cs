// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Diagnostics
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Perf_Process
    {
        private readonly string _nonExistingName = Guid.NewGuid().ToString();
        private int _currentProcessId;
        
        [Benchmark]
        public void GetCurrentProcess() => Process.GetCurrentProcess().Dispose();

        [Benchmark]
        public string GetCurrentProcessName()
        {
            using var process = Process.GetCurrentProcess();
            return process.ProcessName;
        }

        [GlobalSetup(Target = nameof(GetProcessById))]
        public void SetupGetProcessById() => _currentProcessId = Process.GetCurrentProcess().Id;
        
        [Benchmark]
        public void GetProcessById() => Process.GetProcessById(_currentProcessId).Dispose();

        [Benchmark]
        public void GetProcesses()
        {
            foreach (var process in Process.GetProcesses())
            {
                process.Dispose();
            }
        }
        
        [Benchmark]
        public void GetProcessesByName()
        {
            foreach (var process in Process.GetProcessesByName(_nonExistingName))
            {
                process.Dispose();
            }
        }

        private static ProcessStartInfo s_startProcessStartInfo = new ProcessStartInfo() {
            FileName = "whoami", // exists on both Windows and Unix, and has very short output
            RedirectStandardOutput = true, // avoid visible output
            UseShellExecute = false // required by Full Framework
        };

        private Process _startedProcess;

        [Benchmark]
        public void Start()
        {
            _startedProcess = Process.Start(s_startProcessStartInfo);
        }

        [IterationCleanup(Target = nameof(Start))]
        public void CleanupStart()
        {
            if (_startedProcess != null)
            {
                _startedProcess.WaitForExit();
                _startedProcess.Dispose();
                _startedProcess = null;
            }
        }

        [Benchmark]
        public void StartAndWaitForExit()
        {
            using (Process p = Process.Start(s_startProcessStartInfo))
            {
                p.WaitForExit();
            }
        }

        private static readonly ProcessStartInfo s_outputStartInfo = new ProcessStartInfo()
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "sh",
            Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "/c for /L %i in (1,1,1000) do @echo Line %i"
                : "-c \"for i in $(seq 1 1000); do echo Line $i; done\"",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        private static readonly DataReceivedEventHandler s_ignoreOutputLine = static (sender, e) => { };

        [Benchmark]
        public void ReadOutputLineByLine()
        {
            using var process = new Process();
            process.StartInfo = s_outputStartInfo;
            process.OutputDataReceived += s_ignoreOutputLine;
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.OutputDataReceived -= s_ignoreOutputLine;
        }

#if NET
        [Benchmark]
        public async Task ReadOutputToEndAsync()
        {
            using Process process = Process.Start(s_outputStartInfo);
            _ = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
        }
#endif

        [Benchmark]
        public void ReadOutputToEnd()
        {
            using Process process = Process.Start(s_outputStartInfo);
            _ = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
    }
}
