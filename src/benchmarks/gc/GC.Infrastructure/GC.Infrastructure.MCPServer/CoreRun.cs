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

        [McpServerTool(Name = "build_clr_libs"), Description("Builds the CoreCLR runtime and base class libraries for the .NET runtime. This is a prerequisite step before generating CoreRun executables for performance testing and benchmarking.")]
        public async Task<string> BuildCLRAndLibs(
            [Description("The absolute path to the root directory of the .NET runtime repository (e.g., 'C:\\runtime'). This should contain the build.cmd script.")] string runtimeRoot,
            [Description("The build configuration for CoreCLR compilation. Debug includes debugging symbols and assertions, Release is optimized for performance, and Checked includes some debugging features with optimizations. Valid options: Debug, Release, Checked.")] string buildConfig,
            [Description("The target CPU architecture for the build. Determines which instruction set and calling conventions to use. Valid options: x64 (64-bit Intel/AMD), x86 (32-bit Intel/AMD), arm64 (64-bit ARM).")] string arch = "x64")
        {
            if (!ValidBuildConfigs.Contains(buildConfig))
            {
                return $"Invalid build configuration: '{buildConfig}'. Valid options are: {string.Join(", ", ValidBuildConfigs)}. Choose Debug for development with full debugging info, Release for optimized production builds, or Checked for optimized builds with some debugging features.";
            }
            if (!ValidArchs.Contains(arch))
            {
                return $"Invalid architecture: '{arch}'. Valid options are: {string.Join(", ", ValidArchs)}. Use x64 for 64-bit Intel/AMD, x86 for 32-bit Intel/AMD, or arm64 for 64-bit ARM processors.";
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
                        return $"Failed to build {arch} {buildConfig} CoreCLR and Release libraries. Please check the build log output above for detailed error information. Common issues include missing dependencies, incorrect paths, or insufficient permissions.";
                    }
                    else
                    {
                        return $"Successfully built {arch} {buildConfig} CoreCLR and Release libraries. The runtime components are now available for generating CoreRun executables.";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Failed to execute build command '{fileName} {arguments}'. Error: {ex.Message}. Please verify the runtime root path is correct and the build.cmd script exists.";
            }
        }

        [McpServerTool(Name = "generate_corerun"), Description("Generates a CoreRun executable for the .NET runtime. CoreRun is a lightweight host that can run .NET applications without the full SDK, commonly used for performance testing, benchmarking, and isolated runtime scenarios.")]
        public async Task<string> GenerateCoreRun(
            [Description("The absolute path to the root directory of the .NET runtime repository (e.g., 'C:\\runtime'). This should contain the build.cmd script.")] string runtimeRoot,
            [Description("The build configuration for CoreCLR compilation. Debug includes debugging symbols and assertions, Release is optimized for performance, and Checked includes some debugging features with optimizations. Valid options: Debug, Release, Checked.")] string buildConfig,
            [Description("The target CPU architecture for the build. Determines which instruction set and calling conventions to use. Valid options: x64 (64-bit Intel/AMD), x86 (32-bit Intel/AMD), arm64 (64-bit ARM).")] string arch = "x64")
        {
            if (!ValidBuildConfigs.Contains(buildConfig))
            {
                return $"Invalid build configuration: '{buildConfig}'. Valid options are: {string.Join(", ", ValidBuildConfigs)}. Choose Debug for development with full debugging info, Release for optimized production builds, or Checked for optimized builds with some debugging features.";
            }
            if (!ValidArchs.Contains(arch))
            {
                return $"Invalid architecture: '{arch}'. Valid options are: {string.Join(", ", ValidArchs)}. Use x64 for 64-bit Intel/AMD, x86 for 32-bit Intel/AMD, or arm64 for 64-bit ARM processors.";
            }

            string workingDirectory = Path.Combine(runtimeRoot, "src", "tests");
            if (!Directory.Exists(workingDirectory))
            {
                return $"The required directory '{workingDirectory}' does not exist. Please ensure you have a complete .NET runtime repository with the src/tests folder.";
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
                        return $"Failed to generate {arch} {buildConfig} CoreRun executable. Please check the build log output above for detailed error information. Ensure that CoreCLR and libraries were built successfully first using the build_clr_libs tool.";
                    }
                    else
                    {
                        return $"Successfully generated {arch} {buildConfig} CoreRun executable. You can find the CoreRun.exe in the test layout directory for {arch} {buildConfig} configuration.";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Failed to execute CoreRun generation command '{fileName} {arguments}'. Error: {ex.Message}. Please verify the runtime root path is correct and the src/tests/build.cmd script exists.";
            }
        }
    }
}
