// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Diagnostics
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Process
    {
        private readonly string _nonExistingName = Guid.NewGuid().ToString();
        private int _currentProcessId;
        
        [Benchmark]
        public void GetCurrentProcess() => Process.GetCurrentProcess().Dispose();
        
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
            RedirectStandardOutput = true // avoid visible output
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
    }
}
