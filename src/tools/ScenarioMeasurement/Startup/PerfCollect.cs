using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace ScenarioMeasurement
{
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
            string perfCollectScript = Path.Combine(startupDirectory, "perfcollect");
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

            if (Install(logger) != Result.Success)
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

            string args = arguments.Remove(arguments.Length - 1, 1).ToString();

            perfCollectProcess.Arguments = args;
            return perfCollectProcess.Run().Result;
        }

        public Result Stop()
        {
            string arguments = $"stop {TraceName} ";
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

        public Result Install(Logger logger)
        {
            if (LttngInstalled(logger))
            {
                Console.WriteLine("Lttng is already installed.");
                return Result.Success;
            }
            perfCollectProcess.Arguments = "install -force";
            perfCollectProcess.Run();

            int retry = 10;
            for(int i=0; i<retry; i++)
            {
                if (!LttngInstalled(logger))
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

        private bool LttngInstalled(Logger logger)
        {
            ProcessStartInfo procStartInfo = new ProcessStartInfo("bash", "-c lsmod");
            logger.Log("FileName: " + procStartInfo.FileName);
            logger.Log("Args: " + procStartInfo.Arguments);
            Process proc = new Process() { StartInfo = procStartInfo, };
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();
            proc.WaitForExit();
            string result = proc.StandardOutput.ReadToEnd();
            logger.Log("Result: (" + result + ")");
            return File.Exists("//usr/bin/lttng") && result != null;
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
}
