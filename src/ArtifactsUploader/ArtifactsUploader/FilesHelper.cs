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
        public static FileInfo GetNonExistingArchiveFile(DirectoryInfo workplace, string jobName)
        {
            var fileInfo = new FileInfo(
                Path.Combine(
                    workplace.FullName,
                    $"{jobName}{Compressor.FileExtension}")); // I assume that job name is unique

            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            return fileInfo;
        }

        public static IEnumerable<FileInfo> GetFilesToArchive(DirectoryInfo artifactsDirectory, IEnumerable<string> searchPatterns)
            => searchPatterns.SelectMany(searchPattern =>
                    artifactsDirectory.EnumerateFiles(searchPattern, SearchOption.AllDirectories));
    }
}