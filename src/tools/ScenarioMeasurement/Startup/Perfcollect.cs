using Microsoft.Diagnostics.Tracing.Parsers;
using ScenarioMeasurement;
using System;
using System.Collections.Generic;
using System.IO;


namespace ScenarioMeasurement
{
    public class Perfcollect : IDisposable
    {
        private readonly string filepath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, @"Startup/perfcollect");
        private ProcessHelper perfcollectProcess;
        public string TraceName { get; private set; }
        public string TraceDirectory { get; private set; }
        public List<KernelKeyword> KernelEvents { get; set; }
        public List<ClrKeyword> ClrEvents { get; set; }
        public Perfcollect(string traceName, Logger logger) : this (traceName, Environment.CurrentDirectory, logger)
        {
        }

        public Perfcollect(string traceName, string traceDirectory, Logger logger)
        {
            TraceName = traceName;
/*            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"Pefcollect not found at {filepath}. Please rebuild the project to download it.");
            }*/

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
            string arguments = $"start {TraceName} -events ";
            
            foreach (var keyword in KernelEvents)
            {
                arguments += Enum.GetName(typeof(KernelKeyword), keyword)+",";
            }

            foreach (var keyword in ClrEvents)
            {
                arguments += Enum.GetName(typeof(ClrKeyword), keyword)+",";
            }

            arguments.TrimEnd(',');

            perfcollectProcess.Arguments = arguments;
            return ProcessHelper.Result.Success;
            //return perfcollectProcess.Run().Result;
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
            //TODO: move logs to appropriate location
            return result;
        }

        public ProcessHelper.Result Install()
        {
            perfcollectProcess.Arguments = "install -force";
            return perfcollectProcess.Run().Result;
        }

        public void Dispose()
        {
            Stop();
        }

        public enum KernelKeyword{ 
            Empty,
            ProcessLifetime,
            Thread,
            ContextSwitch
        }

        public enum ClrKeyword { 
            Empty,
            Threading,
            DotNETRuntimePrivate_StartupKeyword // TODO: enable perfcollect to take a list of keywords
        }


    }
}
