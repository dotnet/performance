namespace GC.Infrastructure.Core.Analysis
{
    public sealed class ProcessExecutionDetails
    {
        public ProcessExecutionDetails(string key,
                                       string commandlineArgs,
                                       Dictionary<string, string> environmentVariables,
                                       string standardError,
                                       string standardOut,
                                       int exitCode)
        {
            Key = key;
            CommandlineArgs = commandlineArgs;
            EnvironmentVariables = environmentVariables;
            StandardError = standardError;
            StandardOut = standardOut;
            ExitCode = exitCode;
        }

        public string Key { get; }
        public string CommandlineArgs { get; }
        public Dictionary<string, string> EnvironmentVariables { get; }
        public string StandardError { get; }
        public string StandardOut { get; }
        public int ExitCode { get; }
        public bool HasFailed => ExitCode != 0;
    }
}
