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
using System.Collections.Generic;
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
            Dictionary<string, BdnResult> savedFiles = new();

            foreach (ZipArchiveEntry mainArchiveEntry in mainArchive.Entries.Where(e => !e.IsDirectory))
            {
                using Stream zipArchiveEntryCopy = CreateCopy(mainArchiveEntry.OpenEntryStream());

                if (mainArchiveEntry.Key.EndsWith(".zip")) // .zip files from interrupted runs, compressed manually by the contributors
                {
                    using ZipArchive zipArchive = ZipArchive.Open(zipArchiveEntryCopy);
                    foreach (var zipEntry in zipArchive.Entries.Where(e => !e.IsDirectory && !e.Key.EndsWith(".md")))
                    {
                        if (TryGetJsonFileName(output, mainArchiveEntry.Key, zipEntry.Key, zipEntry, savedFiles, out string filePath))
                        {
                            zipEntry.WriteToFile(filePath);
                        }
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
                            foreach (var tarEntry in tarArchive.Entries.Where(e => !e.IsDirectory && !e.Key.EndsWith(".md") && !e.Key.EndsWith("PaxHeader")))
                            {
                                if (TryGetJsonFileName(output, mainArchiveEntry.Key, extractedGzip.Entry.Key, tarEntry, savedFiles, out string filePath))
                                {
                                    tarEntry.WriteToFile(filePath);
                                }
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

        private static bool TryGetJsonFileName(DirectoryInfo output, string mainEntryKey, string currentEntryKey, IArchiveEntry archiveEntry,
            Dictionary<string, BdnResult> savedFiles, out string filePath)
        {
            BdnResult deserialized;
            using Stream jsonFileCopy = CreateCopy(archiveEntry.OpenEntryStream());
            try
            {
                deserialized = Helper.ReadFromStream(jsonFileCopy);
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                filePath = null; // it was not a JSON file (example: markdown file without .md file extension)
                return false;
            }

            string userName = mainEntryKey.Split('/')[2]; // sth like Performance-Runs/nativeaot6.0/adsitnik/arm64_win10-nativeaot6.0.tar.gz
            string moniker = GetMoniker(currentEntryKey) ?? GetMoniker(mainEntryKey);

            StringBuilder sb = new StringBuilder();
            sb.Append(deserialized.HostEnvironmentInfo.Architecture).Append('_');
            sb.Append(Stats.GetSimplifiedOSName(deserialized.HostEnvironmentInfo.OsVersion).Replace(" ", "")).Append('_');
            sb.Append(GetSimplifiedProcessorName(deserialized.HostEnvironmentInfo.ProcessorName).Replace(" ", "")).Append('_');
            sb.Append(userName).Append('_');
            sb.Append(moniker); // netX-previewY

            string outputPath = Path.Combine(output.FullName, sb.ToString().ToLower());
            Directory.CreateDirectory(outputPath);

            // bdnResult.Title exaple: System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Mariomkas-20220504-182513
            string jsonFileName = $"{deserialized.Title.Split('-')[0]}.full.json";
            filePath = Path.Combine(outputPath, jsonFileName);

            if (!savedFiles.TryAdd(filePath, deserialized))
            {
                // we already have such a file, let's check if it's using newer BenchmarkDotNet version
                if (GetVersion(deserialized) < GetVersion(savedFiles[filePath]))
                {
                    return false;
                }

                savedFiles[filePath] = deserialized; // use more recent results
            }

            return true;

            static Version GetVersion(BdnResult bdnResult)
                => Version.Parse(bdnResult.HostEnvironmentInfo.BenchmarkDotNetVersion.Split('-')[0]); // sth like 0.13.1.1786-nightly
        }

        private static string GetMoniker(string key)
        {
            if (key.Contains("net6")) // some files are net6.0, some are missing the dot (net60)
                return "net6.0";
            if (key.Contains("nativeaot6"))
                return "nativeaot6.0";
            if (key.Contains("net7.0-preview"))
                return "net7.0-preview" + key[key.IndexOf("net7.0-preview") + "net7.0-preview".Length];
            if (key.Contains("net7.0-rc"))
                return "net7.0-rc" + key[key.IndexOf("net7.0-rc") + "net7.0-rc".Length];
            if (key.Contains("nativeaot7.0-preview"))
                return "nativeaot7.0-preview" + key[key.IndexOf("nativeaot7.0-preview") + "nativeaot7.0-preview".Length];
            if (key.Contains("net7.0"))
                return "net7.0";
            if (key.Contains("net8.0-preview"))
                return "net8.0-preview" + key[key.IndexOf("net8.0-preview") + "net8.0-preview".Length];
            if (key.Contains("net8.0-rc"))
                return "net8.0-rc" + key[key.IndexOf("net8.0-rc") + "net8.0-rc".Length];
            if (key.Contains("nativeaot8.0-preview"))
                return "nativeaot8.0-preview" + key[key.IndexOf("nativeaot8.0-preview") + "nativeaot8.0-preview".Length];
            if (key.Contains("net8.0"))
                return "net8.0";
            if (key.Contains("net9.0-preview"))
                return "net9.0-preview" + key[key.IndexOf("net9.0-preview") + "net9.0-preview".Length];
            if (key.Contains("net9.0-rc"))
                return "net9.0-rc" + key[key.IndexOf("net9.0-rc") + "net9.0-rc".Length];
            if (key.Contains("nativeaot9.0-preview"))
                return "nativeaot9.0-preview" + key[key.IndexOf("nativeaot9.0-preview") + "nativeaot9.0-preview".Length];
            if (key.Contains("net9.0"))
                return "net9.0";
            if (key.StartsWith("net10.0"))
                return "net10.0";
            if (key.StartsWith("nativeaot10.0"))
                return key;

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
