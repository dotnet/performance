using System.Text;
using System.Diagnostics;

namespace GC.Infrastructure.MCPServer.Utilities
{
    public record CommandResult(int ExitCode, string StdOut, string StdErr, bool IsTimeout = false);

    public class CliCommand
    {
        /// <summary>
        /// Runs a command with a timeout duration. Default timeout is 5 minutes.
        /// </summary>
        /// <param name="filename">The executable filename</param>
        /// <param name="arguments">Command arguments</param>
        /// <param name="workingDirectory">Working directory for the command</param>
        /// <param name="timeout">Timeout duration. Default is 5 minutes.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Command result with IsTimeout flag indicating if timeout occurred</returns>
        public static async Task<CommandResult> RunCommandAsync(
            string filename,
            string arguments,
            string? workingDirectory,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            timeout ??= TimeSpan.FromMinutes(5); // Default 5 minute timeout

            using var timeoutCts = new CancellationTokenSource(timeout.Value);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            try
            {
                return await RunCommandAsync(filename, arguments, workingDirectory, combinedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                // Return a result indicating timeout instead of throwing exception
                return new CommandResult(
                    ExitCode: -1, 
                    StdOut: string.Empty, 
                    StdErr: $"Command '{filename} {arguments}' timed out after {timeout.Value.TotalSeconds} seconds", 
                    IsTimeout: true);
            }
        }

        public static async Task<CommandResult> RunCommandAsync(
            string filename,
            string arguments,
            string? workingDirectory,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(filename, nameof(filename));

            using var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments ?? string.Empty,
                    WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? Directory.GetCurrentDirectory() : workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            var stdoutClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stderrClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var exitTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    outputBuilder.AppendLine(args.Data);
                else
                    stdoutClosed.TrySetResult(true);
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    errorBuilder.AppendLine(args.Data);
                else
                    stderrClosed.TrySetResult(true);
            };

            process.Exited += (_, _) => exitTcs.TrySetResult(process.ExitCode);

            try
            {
                if (!process.Start()) throw new InvalidOperationException($"Failed to start process '{filename}'");

                using var processExitedRegistration = cancellationToken.Register(() =>
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            if (!process.HasExited)
                                process.Kill(entireProcessTree: true);
                        }
                        catch { /* Ignore errors during cancellation */ }
                    });
                });

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(cancellationToken);
                var exitCode = await exitTcs.Task.ConfigureAwait(false);
                return new CommandResult(exitCode, outputBuilder.ToString(), errorBuilder.ToString(), IsTimeout: false);
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation exceptions
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error executing command '{filename} {arguments}': {ex.Message}", ex);
            }
        }
    }
}