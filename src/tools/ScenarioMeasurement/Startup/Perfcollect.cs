using ScenarioMeasurement;
using System;
using System.IO;


namespace Startup
{
    public class Perfcollect
    {
        private readonly string filepath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, @"Startup/perfcollect");
        private ProcessHelper perfcollectProcess;
        public string TraceName { get; private set; }
        public string TraceDirectory { get; private set; }
        public EventOptions Events { get; set; } = EventOptions.Empty;

        public Perfcollect(string traceName, Logger logger) : this (traceName, Environment.CurrentDirectory, logger)
        {
        }

        public Perfcollect(string traceName, string traceDirectory, Logger logger)
        {
            TraceName = traceName;
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"Pefcollect not found at {filepath}. Please rebuild the project to download it.");
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
            

            perfcollectProcess = new ProcessHelper(logger)
            {
                ProcessWillExit = true,
                Executable = filepath,
                Timeout = 300
            };
        }

        public ProcessHelper.Result Start()
        {
            string arguments = $"start {TraceName} ";
            switch (Events)
            {
                case EventOptions.ProcessLifetime:
                    arguments += "-events processlifetime ";
                    break;
                case EventOptions.Threading:
                    arguments += "-events threading ";
                    break;
                case EventOptions.GcCollectOnly:
                    arguments += "-gccollectonly ";
                    break;
                case EventOptions.GcOnly:
                    arguments += "-gconly ";
                    break;
                case EventOptions.GcWithHeap:
                    arguments += "-gcwithheap ";
                    break;
                case EventOptions.Empty: 
                    break;

            }

            perfcollectProcess.Arguments = arguments;
            Console.WriteLine($"arguments: {perfcollectProcess.Arguments}");
            return perfcollectProcess.Run().Result;
        }

        public ProcessHelper.Result Stop()
        {
            string arguments = $"stop {TraceName} ";
            perfcollectProcess.Arguments = arguments;
            var result = perfcollectProcess.Run().Result;

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
            File.Move(traceFile, Path.Combine(TraceDirectory, traceFile));
            return result;
        }

        public ProcessHelper.Result Install()
        {
            perfcollectProcess.Arguments = "install -force";
            return perfcollectProcess.Run().Result;
        }

        public enum EventOptions{ 
            Empty,
            ProcessLifetime,
            Threading,
            GcCollectOnly,
            GcOnly,
            GcWithHeap
        }

    }
}
