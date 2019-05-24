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
        [Option("container", Required = true, HelpText = "Container name")]
        public string ContainerName { get; set; }

        [Option("folderName", Required = false, HelpText = "Folder name within container")]
        public string FolderName { get; set; }

        [Option("archiveName", Required = false, HelpText = "Archive Name")]
        public string ArchiveName { get; set; }

        [Option("searchDirectory", Required = true, HelpText = "Directory to search")]
        public DirectoryInfo SearchDirectory { get; set; }

        [Option("searchPatterns", Required = true, HelpText = "Search patterns", Min = 1)]
        public IEnumerable<string> SearchPatterns { get; set; }

        [Option("storageUrl", Required = true, HelpText = "Url to Azure Blob Storage")]
        public string StorageUrl { get; set; }

        [Option("token", Required = false, HelpText = "SAS Token. NOT KEY!!")]
        public string SasToken { get; set; }

        [Option("timeoutMinutes", Required = false, Default = 10, HelpText = "Timout for upload, in minutes")]
        public int TimeoutInMinutes { get; set; }

        [Option("workplace", Required = false, HelpText = "Path to workplace directory where compressed artifacts will be stored")]
        public DirectoryInfo Workplace { get; set; }

        [Option("skipCompression", Required = false, HelpText = "Skips compression and uploads discovered files individually")]
        public bool SkipCompression { get; set; }

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
            if (!options.SearchDirectory.Exists)
            {
                log.Error($"Provided directory, [{options.SearchDirectory.FullName}] does NOT exist. Unable to upload the artifacts!");
                return false;
            }

            if (!(options.Workplace?.Exists ?? true))
            {
                log.Error($"Provided directory, [{options.Workplace.FullName}] does NOT exist. Unable to upload the artifacts!");
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