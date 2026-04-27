using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Spectre.Console;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace GC.Infrastructure.MCPServer
{
    public class MCPServer
    {
        public static void Main(string[] args)
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

            var builder = Host.CreateApplicationBuilder(args);
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // Configure all logs to go to stderr
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();
            builder.Build().Run();
        }

        [SupportedOSPlatform("windows")]
        internal static bool IsAdministrator =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
