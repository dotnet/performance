using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DockerHarness
{
    // https://stackoverflow.com/a/8946825
    internal sealed class IdentityEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public int GetHashCode(T value)
        {
            return RuntimeHelpers.GetHashCode(value);
        }

        public bool Equals(T left, T right)
        {
            return left == right; // Reference identity comparison
        }
    }

    // A simple class for escaping strings for CSV writing
    // https://stackoverflow.com/a/769713
    // Used instead of a package because only these < 20 lines of code are needed
    internal static class Csv
    {
        public static string Escape(string s)
        {
            if (s.Contains( QUOTE ))
                s = s.Replace(QUOTE, ESCAPED_QUOTE);

            if (s.IndexOfAny(CHARACTERS_THAT_MUST_BE_QUOTED) > -1)
                s = QUOTE + s + QUOTE;

            return s;
        }

        public static StringBuilder AppendCsvRow(this StringBuilder sb, params object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                sb.Append(Escape(objects[i].ToString()));

                if (i != objects.Length - 1)
                {
                    sb.Append(',');
                }
            }
            sb.AppendLine();

            return sb;
        }

        private const string QUOTE = "\"";
        private const string ESCAPED_QUOTE = "\"\"";
        private static char[] CHARACTERS_THAT_MUST_BE_QUOTED = { ',', '"', '\n' };
    }

    internal static class Rfc2822
    {
        private static char[] KeySeperator = new char[]{ ':' };

        public static List<Dictionary<string, string>> Parse(string document)
        {
            var blocks = new List<Dictionary<string, string>>();
            var reader = new StringReader(document);
            string lastKey = null;
            string line = null;
            int lineNum = 0;
            Dictionary<string, string> block = null;

            while((line = reader.ReadLine()) != null)
            {
                // Ignore leading/trailing whitespace
                line = line.Trim();

                if (line != String.Empty && line[0] != '#')
                {
                    if (block == null)
                    {
                        block = new Dictionary<string,string>();
                    }

                    var split = line.Split(KeySeperator, 2);
                    if (split.Length == 2) {
                        var key = split[0].Trim();
                        var val = split[1].Trim();
                        block[key] = val;
                        lastKey = key;
                    }
                    else {
                        Debug.Assert(split.Length == 1);
                        if (lastKey != null) {
                            // This line is a continuation
                            block[lastKey] += split[0];
                        }
                        else {
                            throw new InvalidOperationException($"RFC2822 syntax error. Line {lineNum} is has no key and is not a continuation");
                        }
                    }
                }
                else if (line == String.Empty && block != null)
                {
                    blocks.Add(block);
                    block = null;
                    lastKey = null;
                }

                lineNum++;
            }

            return blocks;
        }
    }

    internal class CommandException : Exception
    {
        public CommandException() { }
        public CommandException(string msg) : base(msg) { }
        public CommandException(string msg, Exception inner) : base(msg, inner) { }
    }

    internal static class Util
    {
        public static void DeleteDirectory(DirectoryInfo dir, bool setAttr=false, uint maxWait=10000)
        {
            dir.Refresh();

            if (!dir.Exists)
            {
                return;
            }

            foreach (var subdir in dir.GetDirectories())
            {
                bool _setAttr = setAttr;
                if (subdir.Name == ".git") {
                    // Let's git aggressive
                    _setAttr = true;
                }
                DeleteDirectory(subdir, setAttr: _setAttr, maxWait: maxWait);
            }

            // Give it time to actually delete all the files
            var files = dir.GetFiles();
            bool wait = true;
            uint waitTime = 0;
            while (wait)
            {
                wait = false;

                foreach (var f in files)
                {
                    if (File.Exists(f.FullName))
                    {
                        try
                        {
                            if (setAttr) {
                                // Allows us to delete read-only files (e.g. git files)
                                f.Attributes = FileAttributes.Normal;
                            }
                            File.Delete(f.FullName);
                        }
                        catch (IOException) { if (waitTime > maxWait) throw; }
                        catch (UnauthorizedAccessException) { if (waitTime > maxWait) throw; }

                        if (File.Exists(f.FullName))
                        {
                            wait = true;

                            // Print a message every 3 seconds if the thread is stuck
                            if (waitTime != 0 && waitTime % 3000 == 0)
                            {
                                Console.WriteLine($"Waiting to delete {f.FullName}");
                            }
                        }
                    }
                }

                // Try again in 100ms
                if (wait)
                {
                    Thread.Sleep(100);
                    waitTime += 100;
                }
            }

            Directory.Delete(dir.FullName);
        }

        private static Random Generator = new Random();
        internal static string RandomString(int length)
        {
            // All alphanumerics except O, 0, 1, and l
            const string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Generator.Next(s.Length)]).ToArray());
        }

        internal static Process Run(string executable, string args, DirectoryInfo workingDir=null)
        {
            Console.WriteLine($"[{workingDir?.FullName ?? Directory.GetCurrentDirectory()}] {executable} {args}");
            return Process.Start(
                new ProcessStartInfo {
                    FileName = executable,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDir?.FullName ?? ""
                }
            );
        }

        internal static StreamReader Command(string executable, string args, DirectoryInfo workingDir=null, bool block=true, Func<Process, bool> handler=null)
        {
            Action<Process> check = (p) => {
                if (p.ExitCode != 0 && (handler == null || !handler(p)))
                {
                    throw new CommandException($"{executable} {args} returned {p.ExitCode}");
                }
            };
          
            Process process;
            if (!block)
            {
                process = Run(executable, args, workingDir);
                process.EnableRaisingEvents = true;
                process.Exited += (sender, e) => {
                    // If handler returns true, then the failure is under control
                    using (process)
                    {
                        check(process);
                    }
                };

                return process.StandardOutput;
            }
            else
            {
                using (process = Run(executable, args, workingDir))
                {
                    process.WaitForExit();
                    check(process);
                    
                    return process.StandardOutput;
                }
            }
        }
    }
}
