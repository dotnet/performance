// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using CommandLine;
using Serilog;

namespace ArtifactsUploader
{
    public class CommandLineOptions
    {
        [Option("project", Required = true, HelpText = "Project name")]
        public string ProjectName { get; set; }

        [Option("branch", Required = true, HelpText = "Branch name")]
        public string BranchName { get; set; }

        [Option("job", Required = true, HelpText = "CI Job name, uploaded archive will have the same name!")]
        public string JobName { get; set; }

        [Option("isPR", Required = true, HelpText = "True for PRs")]
        public bool IsPr { get; set; }

        [Option("artifacts", Required = true, HelpText = "Path to artifacts directory")]
        public DirectoryInfo ArtifactsDirectory { get; set; }

        [Option("searchPatterns", Required = true, HelpText = "Search patterns", Min = 1)]
        public IEnumerable<string> SearchPatterns { get; set; }

        [Option("storageUrl", Required = false, Default = @"https://dotnetperfciblobs.blob.core.windows.net", HelpText = "Url to Azure Blob Storage")]
        public string StorageUrl { get; set; }

        [Option("token", Required = false, HelpText = "SAS Token. NOT KEY!!")]
        public string SasToken { get; set; }

        [Option("timeoutMinutes", Required = false, Default = 10, HelpText = "Timout for upload, in minutes")]
        public int TimeoutInMinutes { get; set; }

        public static (bool isSuccess, CommandLineOptions options) Parse(string[] args, ILogger log)
        {
            (bool isSuccess, CommandLineOptions options) result = default;

            using (var parser = CreateParser(log))
            {
                parser
                    .ParseArguments<CommandLineOptions>(args)
                    .WithParsed(options => result = (Validate(options, log), options))
                    .WithNotParsed(errors => result = (false, default));
            }

            return result;
        }

        private static bool Validate(CommandLineOptions options, ILogger log)
        {
            if (!options.ArtifactsDirectory.Exists)
            {
                log.Error($"Provided directory, [{options.ArtifactsDirectory.FullName}] does NOT exist. Unable to upload the artifacts!");
                return false;
            }

            if (options.TimeoutInMinutes <= 0)
            {
                log.Error($"Provided timeout, [{options.TimeoutInMinutes}] is not a positive number. Unable to upload the artifacts!");
                return false;
            }

            return true;
        }

        private static Parser CreateParser(ILogger log)
            => new Parser(settings =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = false;
                settings.EnableDashDash = true;
                settings.IgnoreUnknownArguments = false;
                settings.HelpWriter = new LogWrapper(log);
            });
    }
}