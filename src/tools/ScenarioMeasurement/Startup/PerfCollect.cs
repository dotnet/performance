using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Text;

namespace ScenarioMeasurement
{
    public class PerfCollect : IDisposable
    {
        private readonly string startupDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private ProcessHelper perfCollectProcess;
        public string TraceName { get; private set; }
        public string TraceDirectory { get; private set; }
        public string TraceFilePath { get; private set; }
        private List<KernelKeyword> KernelEvents = new List<KernelKeyword>();
        private List<ClrKeyword> ClrEvents = new List<ClrKeyword>();
        public PerfCollect(string traceName, Logger logger) : this(traceName, Environment.CurrentDirectory, logger)
        {
        }

        public PerfCollect(string traceName, string traceDirectory, Logger logger)
        {
            TraceName = traceName;
            string perfCollectScript = Path.Combine(startupDirectory, "perfcollect");
            if (!File.Exists(perfCollectScript))
            {
                throw new FileNotFoundException($"Pefcollect not found at {perfCollectScript}. Please rebuild the project to download it.");
            }

            if (String.IsNullOrEmpty(traceName))
            {
                throw new ArgumentException("Trace file name cannot be empty.");
            }


            if (!Directory.Exists(traceDirectory))
            {
                Directory.CreateDirectory(traceDirectory);
            }
            TraceDirectory = traceDirectory;


            perfCollectProcess = new ProcessHelper(logger)
            {
                ProcessWillExit = true,
                Executable = perfCollectScript,
                Timeout = 300
            };
        }

        public ProcessHelper.Result Start()
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

        public ProcessHelper.Result Stop()
        {
            string arguments = $"stop {TraceName} ";
            perfCollectProcess.Arguments = arguments;
            var result = perfCollectProcess.Run().Result;

            string traceFile = $"{TraceName}.trace.zip";
            if (!File.Exists(traceFile))
            {
                throw new FileNotFoundException("Trace file not found.");
            }
            string destinationFile = Path.Combine(TraceDirectory, traceFile);
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }
            TraceFilePath = Path.Combine(TraceDirectory, traceFile);
            File.Move(traceFile, TraceFilePath);

            //TODO: move logs to appropriate location
            return result;
        }

        public ProcessHelper.Result Install()
        {
            perfCollectProcess.Arguments = "install -force";
            return perfCollectProcess.Run().Result;
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

        public enum KernelKeyword
        {
            Empty,
            ProcessLifetime,
            Thread,
            ContextSwitch
        }

        public enum ClrKeyword
        {
            Empty,
            Threading,
            DotNETRuntimePrivate_StartupKeyword // TODO: enable perfCollect to take a list of keywords
        }


    }
}
