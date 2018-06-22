// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Serilog;

namespace ArtifactsUploader
{
    public static class Compressor
    {
        public static string FileExtension
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ".zip"
                : ".tar";

        public static void Compress(FileInfo archive, IEnumerable<FileInfo> filesToCompress, ILogger log)
        {
            log.Information($"Creating {archive.FullName}");

            using (var zip = ZipFile.Open(archive.FullName, ZipArchiveMode.Create))
            {
                foreach (var file in filesToCompress)
                {
                    log.Information($"Adding {file.FullName} to the archive");

                    zip.CreateEntryFromFile(file.FullName, file.Name, CompressionLevel.Optimal);
                }
            }
        }
    }
}