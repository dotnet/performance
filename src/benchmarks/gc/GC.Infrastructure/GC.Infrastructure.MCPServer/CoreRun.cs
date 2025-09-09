using System.ComponentModel;
using System.Diagnostics;
using ModelContextProtocol.Server;

namespace GC.Infrastructure.MCPServer
{
    [McpServerToolType]
    internal class CoreRun
    {
        private static readonly string[] ValidBuildConfigs = new string[] { "Debug", "Release", "Checked" };
        private static readonly string[] ValidArchs = new string[] { "x64", "x86", "arm64" };

        [McpServerTool(Name = "build_clr_libs"), Description("Build clr and libs.")]
        public async Task<string> BuildCLRAndLibs(string runtimeRoot, string buildConfig, string arch = "x64")
        {
            if (!ValidBuildConfigs.Contains(buildConfig))
            {
                return $"Invalid build configuration: {buildConfig}. Valid options are Debug, Release, Checked.";
            }
            if (!ValidArchs.Contains(arch))
            {
                return $"Invalid arch: {arch}. Valid options are x64, x86, arm64.";
            }

            string fileName = "cmd.exe";
            string arguments = $"/C build.cmd clr+libs -runtimeConfiguration {buildConfig} -librariesConfiguration Release -arch {arch}";
            try
            {
                bool isSuccess = true;
                using (var process = new Process())
                {
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WorkingDirectory = runtimeRoot;
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            if (e.Data.ToLower().Contains("build failed with exit code"))
                            {
                                isSuccess = false;
                            }
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            if (e.Data.ToLower().Contains("build failed with exit code"))
                            {
                                isSuccess = false;
                            }
                        }
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();

                    if (!isSuccess)
                    {
                        return "Fail to build coreclr and libs, please check the build log for more details.";
                    }
                    else
                    {
                        return "Successfully build coreclr and libs";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Fail to run command `{fileName} {arguments}`: {ex.Message}";
            }
        }

        [McpServerTool(Name = "generate_corerun"), Description("Generate Corerun.")]
        public async Task<string> GenerateCoreRun(string runtimeRoot, string buildConfig, string arch = "x64")
        {
            if (!ValidBuildConfigs.Contains(buildConfig))
            {
                return $"Invalid build configuration: {buildConfig}. Valid options are Debug, Release, Checked.";
            }
            if (!ValidArchs.Contains(arch))
            {
                return $"Invalid arch: {arch}. Valid options are x64, x86, arm64.";
            }

            string workingDirectory = Path.Combine(runtimeRoot, "src", "tests");
            if (!Directory.Exists(workingDirectory))
            {
                return $"The directory {workingDirectory} does not exist.";
            }
            string fileName = "cmd.exe";
            string arguments = $"/C build.cmd generatelayoutonly {arch} {buildConfig}";
            try
            {
                bool isSuccess = true;
                using (var process = new Process())
                {
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WorkingDirectory = workingDirectory;
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            if (e.Data.ToLower().Contains("build failed with exit code"))
                            {
                                isSuccess = false;
                            }
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            if (e.Data.ToLower().Contains("build failed with exit code"))
                            {
                                isSuccess = false;
                            }
                        }
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();

                    if (!isSuccess)
                    {
                        return "Fail to build corerun, please check the build log for more details.";
                    }
                    else
                    {
                        return "Successfully build corerun";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Fail to run command `{fileName} {arguments}`: {ex.Message}";
            }
        }
    }
}
