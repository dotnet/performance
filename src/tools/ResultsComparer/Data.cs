// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using DataTransferContracts;
using SharpCompress.Archives;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ResultsComparer
{
    internal static class Data
    {
        internal static void Decompress(FileInfo zip, DirectoryInfo output)
        {
            using Stream mainZipStream = zip.OpenRead();
            using ZipArchive mainArchive = ZipArchive.Open(mainZipStream);
            string folderPath;

            foreach (ZipArchiveEntry mainArchiveEntry in mainArchive.Entries.Where(e => !e.IsDirectory))
            {
                folderPath = null;
                using Stream zipArchiveEntryCopy = CreateCopy(mainArchiveEntry.OpenEntryStream());

                if (mainArchiveEntry.Key.EndsWith(".zip")) // .zip files from interrupted runs, compressed manually by the contributors
                {
                    using ZipArchive zipArchive = ZipArchive.Open(zipArchiveEntryCopy);
                    foreach (var zipEntry in zipArchive.Entries.Where(e => e.Key.EndsWith(".json")))
                    {
                        zipEntry.WriteToDirectory(folderPath ??= GetFolderPath(output, mainArchiveEntry.Key, zipEntry.Key, zipEntry));
                    }
                }
                else if (mainArchiveEntry.Key.EndsWith(".tar.gz") // produced by benchmarks_monthly.py
                    && !mainArchiveEntry.Key.Contains("SSE")) // temporary workaround (TODO: handle results for same config with different env vars)
                {
                    using GZipArchive gZipArchive = GZipArchive.Open(zipArchiveEntryCopy);
                    using IReader extractedGzip = gZipArchive.ExtractAllEntries();
                    while (extractedGzip.MoveToNextEntry())
                    {
                        if (!extractedGzip.Entry.IsDirectory)
                        {
                            using Stream tarCopy = CreateCopy(extractedGzip.OpenEntryStream());
                            using TarArchive tarArchive = TarArchive.Open(tarCopy);
                            foreach (var tarEntry in tarArchive.Entries.Where(e => e.Key.EndsWith(".json")))
                            {
                                tarEntry.WriteToDirectory(folderPath ??= GetFolderPath(output, mainArchiveEntry.Key, extractedGzip.Entry.Key, tarEntry));
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"{mainArchiveEntry.Key} was not recognized");
                }
            }
        }

        // most of the entry streams are non-seekable and it causes trouble, this is why we just create copies
        private static Stream CreateCopy(Stream entryStream)
        {
            using (entryStream)
            {
                MemoryStream copy = new();
                entryStream.CopyTo(copy);
                copy.Position = 0;
                return copy;
            }
        }

        private static string GetFolderPath(DirectoryInfo output, string mainEntryKey, string currentEntryKey, IArchiveEntry archiveEntry)
        {
            using Stream jsonFileCopy = CreateCopy(archiveEntry.OpenEntryStream());
            BdnResult bdnResult = Helper.ReadFromStream(jsonFileCopy);

            string userName = mainEntryKey.Split('/')[2]; // sth like Performance-Runs/nativeaot6.0/adsitnik/arm64_win10-nativeaot6.0.tar.gz
            string moniker = GetMoniker(currentEntryKey) ?? GetMoniker(mainEntryKey);

            StringBuilder sb = new StringBuilder();
            sb.Append(bdnResult.HostEnvironmentInfo.Architecture).Append('_');
            sb.Append(Stats.GetSimplifiedOSName(bdnResult.HostEnvironmentInfo.OsVersion).Replace(" ", "")).Append('_');
            sb.Append(GetSimplifiedProcessorName(bdnResult.HostEnvironmentInfo.ProcessorName).Replace(" ", "")).Append('_');
            sb.Append(userName).Append('_');
            sb.Append(moniker); // netX-previewY

            string outputPath = Path.Combine(output.FullName, sb.ToString().ToLower());
            Directory.CreateDirectory(outputPath);
            return outputPath;
        }

        private static string GetMoniker(string key)
        {
            if (key.Contains("net6")) // some files are net6.0, some are missing the dot (net60)
                return "net6.0";
            if (key.Contains("nativeaot6"))
                return "nativeaot6.0";
            if (key.Contains("net7.0-preview"))
                return "net7.0-preview" + key[key.IndexOf("net7.0-preview") + "net7.0-preview".Length];
            if (key.Contains("nativeaot7.0-preview"))
                return "nativeaot7.0-preview" + key[key.IndexOf("nativeaot7.0-preview") + "nativeaot7.0-preview".Length];

            return null;
        }

        // This method is hacky and ugly. Don't use it anywhere else!!
        // We could just get hash code of the processor name and be done with it,
        // but then during actual investigation it would be harder to find the json files with original results.
        private static string GetSimplifiedProcessorName(string processorName)
        {
            if (processorName.StartsWith("Unknown"))
                return "unknown";

            foreach (var segment in processorName.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (segment[0] == 'i' || segment[0] == 'E') // things like i7-8700 or E5530
                    return segment.Replace("-", "");
                if (segment.EndsWith("CL")
                    || segment.EndsWith("X") // things like AMD 3945WX or 5900X 
                    || segment.StartsWith("SQ") // things like SQ1
                    || segment.StartsWith("M1")
                    || segment.StartsWith("ARM")) // things like ARMv7
                {
                    return segment;
                }
            }

            return processorName.Replace(" ", "");
        }
    }
}