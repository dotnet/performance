using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;

namespace GC.Infrastructure.MCPServer
{
    [McpServerToolType]
    internal class CoreRun
    {
        [McpServerTool(Name = "build_clr_libs"), Description("Build clr and libs.")]
        public int BuildCLRAndLibs(string runtimeRoot, string buildConfig, string arch = "x64")
        {
            if (!CheckBuildConfig(buildConfig))
            {
                throw new ArgumentException($"Invalid build configuration: {buildConfig}. Valid options are Debug, Release, Checked.");
            }
            if (!CheckArch(arch))
            {
                throw new ArgumentException($"Invalid arch: {arch}. Valid options are x64, x86, arm64.");
            }
            using (var process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C build.cmd clr+libs -runtimeConfiguration {buildConfig} -librariesConfiguration Release -arch {arch}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = runtimeRoot;
                process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                process.ErrorDataReceived += (sender, args) => Console.Error.WriteLine(args.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        [McpServerTool(Name = "generate_core_root"), Description("Generate Core_Root.")]
        public int GenerateCoreRoot(string runtimeRoot, string buildConfig, string arch = "x64")
        {
            if (!CheckBuildConfig(buildConfig))
            {
                throw new ArgumentException($"Invalid build configuration: {buildConfig}. Valid options are Debug, Release, Checked.");
            }
            if (!CheckArch(arch))
            {
                throw new ArgumentException($"Invalid arch: {arch}. Valid options are x64, x86, arm64.");
            }

            string workingDirectory = Path.Combine(runtimeRoot, "src", "tests");
            if (!Directory.Exists(workingDirectory))
            {
                throw new DirectoryNotFoundException($"The directory {workingDirectory} does not exist.");
            }
            using (var process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C build.cmd generatelayoutonly {arch} {buildConfig}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                process.ErrorDataReceived += (sender, args) => Console.Error.WriteLine(args.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        private bool CheckBuildConfig(string buildConfig)
        {
            string[] validConfigs = { "Debug", "Release", "Checked" };
            return validConfigs.Contains(buildConfig);
        }

        private bool CheckArch(string arch)
        {
            string[] validArchs = { "x64", "x86", "arm64" };
            return validArchs.Contains(arch);
        }
    }
}
