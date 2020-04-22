using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ScenarioMeasurement
{
    public class ProcessHelper
    {
        /// <summary>
        /// Specifies whether the app exits on its own
        /// true: App will exit, Timeout specifies how long to wait before terminating the process.
        /// false: App will be closed after MeasurementDelay.
        /// </summary>
        public bool ProcessWillExit { get; set; } = false;
        /// <summary>
        /// Max time to wait for app to close (seconds)
        /// </summary>
        public int Timeout { get; set; } = 60;
        /// <summary>
        /// Time to wait before closing app (seconds)
        /// </summary>
        public int MeasurementDelay { get; set; } = 15;

        public string Executable { get; set; }

        public string Arguments { get; set; } = String.Empty;
        public string WorkingDirectory { get; set; } = String.Empty;

        public bool GuiApp { get; set; } = false;

        public Logger Logger;

        public Dictionary<string, string> EnvironmentVariables = null;

        public bool RootAccess { get; set; } = false;

        public ProcessHelper(Logger logger)
        {
            this.Logger = logger;
        }

        /// <summary>
        /// Runs the specified process and waits for it to exit.
        /// </summary>
        /// <param name="appExe">Full path to executable</param>
        /// <param name="appArgs">Optional arguments</param>
        /// <param name="workingDirectory">Optional working directory (defaults to current directory)</param>
        /// <returns></returns>
        public (Result Result, int Pid) Run()
        {
            var psi = new ProcessStartInfo();
            if (!Util.IsWindows() && RootAccess)
            {
                psi.FileName = "sudo";
                psi.Arguments = Executable + " " + Arguments;
            }
            else
            {
                psi.FileName = Executable;
                psi.Arguments = Arguments;
            }
            psi.WorkingDirectory = WorkingDirectory;
            // WindowStyles only get passed through if UseShellExecute=true
            // As we only care about WindowStyles for GUI apps, we can use that value here.
            psi.UseShellExecute = GuiApp;

            if (EnvironmentVariables != null)
            {
                foreach (var pair in EnvironmentVariables)
                {
                    psi.EnvironmentVariables[pair.Key] = pair.Value;
                    Logger.Log($"Added environment variable: {pair.Key}={pair.Value}");
                }
            }

            if (!GuiApp)
            {
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
            }
            else
            {
                psi.WindowStyle = ProcessWindowStyle.Maximized;
            }
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            using (var process = new Process())
            {
                process.StartInfo = psi;
                if (!GuiApp)
                {
                    process.OutputDataReceived += (s, e) =>
                    {
                        output.AppendLine(e.Data);
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        error.AppendLine(e.Data);
                    };
                }
                process.Start();
                int pid = process.Id;
                if (!GuiApp)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                if (ProcessWillExit)
                {
                    bool exited = process.WaitForExit(Timeout * 1000);
                    if (!exited)
                    {
                        process.Kill();
                        return (Result.TimeoutExceeded, pid);
                    }
                }
                else
                {
                    Thread.Sleep(MeasurementDelay * 1000);
                    if (!process.HasExited) { process.CloseMainWindow(); }
                    else
                    {
                        return (Result.ExitedEarly, pid);
                    }
                    bool exited = process.WaitForExit(5000);
                    if (!exited)
                    {
                        process.Kill();
                        return (Result.CloseFailed, pid);
                    }
                }

                Logger.Log(output.ToString());
                Logger.Log(error.ToString());

                // Be aware a successful exit could be non-zero
                if (process.ExitCode != 0)
                {
                    return (Result.ExitedWithError, pid);
                }
                return (Result.Success, pid);
            }
        }
        public enum Result
        {
            Success,
            TimeoutExceeded,
            ExitedEarly,
            CloseFailed,
            ExitedWithError
        }
    }
}
