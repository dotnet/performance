using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GC.Infrastructure.Core.Functionality
{
    public class CommandInvokeResult
    {
        public string Command { get; init; }
        public string StandardOutput { get; init; }
        public string StandardError { get; init; }
        public int ProcessID { get; init; }
        public Exception? Exn { get; init; }

        public CommandInvokeResult(string command, string stdout, string stderr, int pid, Exception? ex = null)
        {
            this.Command = command;
            this.StandardOutput = stdout;
            this.StandardError = stderr;
            this.ProcessID = pid;
            this.Exn = ex;
        }
    }

    //public static class CommandInvokeTaskRunner
    //{
    //    public static void Run(string loggerPath,
    //                           IEnumerable<CommandInvokeResult> commandInvokeTask,
    //                           bool ignoreError = false)
    //    {
    //        IEnumerator<CommandInvokeResult> enumrator = commandInvokeTask.GetEnumerator();
    //        while (true)
    //        {
    //            try
    //            {
    //                if (!enumrator.MoveNext())
    //                {
    //                    // Break when move to end
    //                    break;
    //                }
    //                CommandInvokeResult result = enumrator.Current;
    //                StringBuilder logContent = new();
    //                logContent.AppendLine($"Run Command: {result.Command}");
    //                logContent.AppendLine(result.StandardOutput);
    //                logContent.AppendLine(result.StandardError);
    //                if (result.Exn != null)
    //                {
    //                    logContent.AppendLine($"Error Message:{result.Exn.Message}");
    //                    logContent.AppendLine($"Stack Trace:\n{result.Exn.StackTrace}");
    //                    logContent.AppendLine($"Inner Exception:\n:{result.Exn.InnerException}");
    //                }
    //                logContent.AppendLine("\n");
    //                File.AppendAllText(loggerPath, logContent.ToString());

    //                if (!String.IsNullOrEmpty(result.StandardError) || result.Exn != null)
    //                {
    //                    if (!ignoreError)
    //                    {
    //                        Console.WriteLine($"Run Command {result.Command} but get error! See {loggerPath} for details.");
    //                        break;
    //                    }
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                StringBuilder logContent = new();
    //                logContent.AppendLine($"Run into error: {ex.Message}");
    //                logContent.AppendLine($"Stack Trace:\n{ex.StackTrace}");
    //                logContent.AppendLine($"Inner Exception:\n{ex.InnerException}");
    //                File.AppendAllText(loggerPath, logContent.ToString());
    //                break;
    //            }
    //        }
    //    }

    //    public static void RecordSingle(string loggerPath, CommandInvokeResult result)
    //    {
    //        StringBuilder logContent = new();
    //        logContent.AppendLine($"Run Command: {result.Command}");
    //        logContent.AppendLine(result.StandardOutput);
    //        logContent.AppendLine(result.StandardError);
    //        if (result.Exn != null)
    //        {
    //            logContent.AppendLine($"Error Message:{result.Exn.Message}");
    //            logContent.AppendLine($"Stack Trace:\n{result.Exn.StackTrace}");
    //            logContent.AppendLine($"Inner Exception:\n:{result.Exn.InnerException}");
    //        }
    //        logContent.AppendLine("\n");
    //        File.AppendAllText(loggerPath, logContent.ToString());
    //    }
    //}

    public class CommandInvoker : Process, IDisposable
    {
        public static void PrintReceivedData(object sender, DataReceivedEventArgs args)
        {
            string? content = args.Data;
            if (!String.IsNullOrEmpty(content))
            {
                Console.WriteLine($"    {content}");
            }
        }

        private bool IsInvoked = false;
        private StringBuilder stdout;
        private StringBuilder stderr;

        public readonly string Command;
        public readonly int ProcessID;
        public readonly Exception? Exn;

        public string ConsoleOutput
        {
            get { return stdout.ToString(); }
        }

        //public string ConsoleError
        //{
        //    get { return stderr.ToString(); }
        //}

        public CommandInvoker(string fileName,
                              string argument,
                              Dictionary<string, string> env,
                              string workDirectory = "",
                              bool redirectStdOutErr = true,
                              bool silent = false) : base()
        {
            // Initialize
            stdout = new();
            stderr = new();
            Command = $"{fileName} {argument}";

            this.StartInfo.FileName = fileName;
            this.StartInfo.Arguments = argument;
            this.StartInfo.UseShellExecute = false;
            this.StartInfo.RedirectStandardInput = true;
            this.StartInfo.WorkingDirectory = workDirectory;

            foreach (var key in env.Keys)
            {
                this.StartInfo.EnvironmentVariables[key] = env[key];
            }

            if (IsInvoked)
            {
                throw new Exception($"{nameof(CommandInvoker)}: Command \"{Command}\" has been invoked");
            }

            IsInvoked = true;
            if (redirectStdOutErr)
            {
                this.OutputDataReceived += (sender, args) =>
                {
                    string? line = args.Data;
                    if (!String.IsNullOrEmpty(line))
                    {
                        stdout.AppendLine(line);
                    }
                };
                this.ErrorDataReceived += (sender, args) =>
                {
                    string? line = args.Data;
                    if (!String.IsNullOrEmpty(line))
                    {
                        stderr.AppendLine(line);
                    }
                };
            }

            if (!silent)
            {

                this.OutputDataReceived += PrintReceivedData;
                this.ErrorDataReceived += PrintReceivedData;
            }

            this.StartInfo.RedirectStandardOutput = redirectStdOutErr;
            this.StartInfo.RedirectStandardError = redirectStdOutErr;

            Console.WriteLine($"\nRun command: {Command}");

            try
            {
                this.Start();
                ProcessID = this.Id;
                if (redirectStdOutErr)
                {
                    this.BeginOutputReadLine();
                    this.BeginErrorReadLine();
                }
            }
            catch (Exception ex)
            {
                Exn = ex;
            }
        }

        public CommandInvokeResult WaitForResult()
        {
            try
            {
                this.WaitForExit();
                return new(Command, ConsoleOutput, stderr.ToString(), ProcessID);
            }
            catch (Exception ex)
            {
                return new(Command, ConsoleOutput, stderr.ToString(), -1, ex);
            }
        }

        public CommandInvokeResult TerminateForResult()
        {
            try
            {
                this.Kill(true);
                this.WaitForExit();
                return new(Command, ConsoleOutput, stderr.ToString(), ProcessID);
            }
            catch (Exception ex)
            {
                return new(Command, ConsoleOutput, stderr.ToString(), -1, ex);
            }
        }
    }
}
