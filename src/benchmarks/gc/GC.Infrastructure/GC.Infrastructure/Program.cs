using GC.Infrastructure.Commands.ASPNetBenchmarks;
using GC.Infrastructure.Commands.GCPerfSim;
using GC.Infrastructure.Commands.Microbenchmark;
using GC.Infrastructure.Commands.ReliabilityFrameworkTest;
using GC.Infrastructure.Commands.RunCommand;
using Microsoft.Win32;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace GC.Infrastructure
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            RegistryKey? registrykeyHKLM = null;
            string? keyPath = null;
            string? oldValue = null;

            if (OperatingSystem.IsWindows())
            {
                if (!IsAdministrator)
                {
                    AnsiConsole.WriteLine("Not running in admin mode - please elevate privileges to run this process.");
                    return;
                }

                registrykeyHKLM = Registry.CurrentUser;
                keyPath = @"Software\Microsoft\Windows\Windows Error Reporting\DontShowUI";
                oldValue = registrykeyHKLM.GetValue(keyPath)?.ToString() ?? 0x0.ToString();
                registrykeyHKLM.SetValue(keyPath, 0x1, RegistryValueKind.DWord);
            }

            // TODO: Do the same thing for Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting

            AnsiConsole.Write(new FigletText("GC Infrastructure").Centered().Color(Color.Red));

            try
            {
                var app = new CommandApp();
                app.Configure((configuration) =>
                {
                    // Run 
                    configuration.AddCommand<RunCommand>("run");
                    configuration.AddCommand<CreateSuitesCommand>("createsuites");
                    configuration.AddCommand<RunSuiteCommand>("run-suite");

                    // GC PerfSim
                    configuration.AddCommand<GCPerfSimCommand>("gcperfsim");
                    configuration.AddCommand<GCPerfSimAnalyzeCommand>("gcperfsim-analyze");
                    configuration.AddCommand<GCPerfSimCompareCommand>("gcperfsim-compare");
                    configuration.AddCommand<GCPerfSimFunctionalCommand>("gcperfsim-functional");

                    // Microbenchmarks
                    configuration.AddCommand<MicrobenchmarkCommand>("microbenchmarks");
                    configuration.AddCommand<MicrobenchmarkAnalyzeCommand>("microbenchmarks-analyze");

                    // ASP.NET Benchmarks
                    configuration.AddCommand<AspNetBenchmarksCommand>("aspnetbenchmarks");
                    configuration.AddCommand<AspNetBenchmarksAnalyzeCommand>("aspnetbenchmarks-analyze");

                    // ReliabilityFramework
                    configuration.AddCommand<ReliabilityFrameworkTestAnalyzeCommand>("rftest-analyze");
                    configuration.AddCommand<ReliabilityFrameworkTestAggregateCommand>("rftest-aggregate");
                });

                app.Run(args);
            }

            // TODO: Handle each exception.
            catch (Exception)
            {
                throw;
            }

            finally
            {
                if (OperatingSystem.IsWindows())
                {
                    registrykeyHKLM!.SetValue(keyPath, Convert.ToInt16(oldValue), RegistryValueKind.DWord);
                    registrykeyHKLM.Close();
                }
            }
        }

        [SupportedOSPlatform("windows")]
        internal static bool IsAdministrator =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    }
}