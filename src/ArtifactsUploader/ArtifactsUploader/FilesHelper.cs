// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArtifactsUploader
{
    internal static class FilesHelper
    {
        public static FileInfo GetNonExistingArchiveFile(CommandLineOptions options)
        {
            var fileInfo = new FileInfo(
                Path.Combine(
                    Directory.GetCurrentDirectory(), // I assume that I can create file here and CI is going to cleanup everything for me
                    $"{options.JobName}{Compressor.FileExtension}")); // I assume that job name is unique

            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            return fileInfo;
        }

        public static IEnumerable<FileInfo> GetFilesToArchive(CommandLineOptions options)
            => options.SearchPatterns.SelectMany(searchPattern =>
                    options.ArtifactsDirectory.EnumerateFiles(searchPattern, SearchOption.AllDirectories));
    }
}