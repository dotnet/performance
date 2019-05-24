// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;

namespace ArtifactsUploader
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var log = CreateLogger();

            try
            {
                var (isSuccess, commandLineOptions) = CommandLineOptions.Parse(args, log);

                if (!isSuccess)
                {
                    return -1;
                }

                var fileToArchive = FilesHelper.GetFilesToUpload(commandLineOptions.SearchDirectory, commandLineOptions.SearchPatterns);
                if (!fileToArchive.Any())
                {
                    log.Warning("Nothing to compress and upload");
                    return -1;
                }

                if (!commandLineOptions.SkipCompression)
                {
                    var archive = FilesHelper.GetNonExistingArchiveFile(commandLineOptions.Workplace, commandLineOptions.ArchiveName);
                    Compressor.Compress(archive, fileToArchive, log);
                    fileToArchive = new[] { archive };
                }

                using (var tokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(commandLineOptions.TimeoutInMinutes)))
                {
                    await Uploader.Upload(fileToArchive, commandLineOptions, log, tokenSource.Token);
                }

                log.Information("Done!");

                return 0;
            }
            catch (Exception ex)
            {
                log.Error(ex, "Unhandled exception during upload!");

                return -1;
            }
            finally
            {
                log?.Dispose();
            }
        }

        private static Logger CreateLogger()
            => new LoggerConfiguration()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();
    }
}