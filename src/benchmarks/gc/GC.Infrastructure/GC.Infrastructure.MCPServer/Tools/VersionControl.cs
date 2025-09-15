﻿using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;

namespace GC.Infrastructure.MCPServer.Tools
{
    [McpServerToolType]
    internal class VersionControl
    {
        [McpServerTool(Name = "checkout_branch"), Description("Switch to specific branch.")]
        public async Task<string> CheckoutBranch(
            [Description("The root directory of the runtime.")] string runtimeRoot,
            [Description("The name of branch.")] string branchName)
        {
            string fileName = "git";
            string arguments = $"checkout {branchName}";
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
                            if (e.Data.Contains("error:"))
                            {
                                isSuccess = false;
                            }
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            if (e.Data.Contains("error:"))
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
                        return $"Fail to switch to {branchName}, please check the git log for more details.";
                    }
                    else
                    {
                        return $"Successfully switch to {branchName}";
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
