using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Scenarios.Utilities
{
    internal static class GitTasks
    {
        private static readonly HashSet<int> DefaultExitCodes = new HashSet<int>(new[] { 0 });

        internal static Task Clone(string repoUrl, string repoRootDirectory, ITestOutputHelper output)
            => ExecuteCommand($"clone {repoUrl} {repoRootDirectory}", output);

        internal static Task Checkout(string commitShaId, ITestOutputHelper output, string workingDirectory)
            => ExecuteCommand($"checkout {commitShaId}", output, workingDirectory);

        async static Task ExecuteCommand(string arguments, ITestOutputHelper output, string workingDirectory = null)
        {
            int exitCode = await new ProcessRunner("git", arguments).WithLog(output).WithWorkingDirectory(workingDirectory).Run();

            if (!DefaultExitCodes.Contains(exitCode))
                throw new Exception($"git {arguments} has failed, the exit code was {exitCode}");
        }
    }
}
