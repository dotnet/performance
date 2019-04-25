// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompilerBenchmarks
{
    public static class FileTasks
    {
        public async static Task DownloadAndUnzip(string remotePath, string localExpandedDirPath, bool deleteTempFiles=true)
        {
            string tempFileNameBase = Guid.NewGuid().ToString();
            string tempDownloadPath = Path.Combine(Path.GetTempPath(), tempFileNameBase + Path.GetExtension(remotePath));
            Download(remotePath, tempDownloadPath);
            await Unzip(tempDownloadPath, localExpandedDirPath, true);
        }

        public static void Download(string remotePath, string localPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(localPath));
            using (var client = new HttpClient())
            {
                using (FileStream localStream = File.Create(localPath))
                {
                    using (Stream stream = client.GetStreamAsync(remotePath).Result)
                        stream.CopyTo(localStream);
                    localStream.Flush();
                }
            }
        }

        public static async Task Unzip(string zipPath, string expandedDirPath, bool deleteZippedFiles=true, string tempTarPath=null)
        {
            await FileTasks.UnWinZip(zipPath, expandedDirPath);
            if (deleteZippedFiles)
            {
                File.Delete(zipPath);
            }
        }

        public static async Task UnWinZip(string zipPath, string expandedDirPath)
        {
            using (FileStream zipStream = File.OpenRead(zipPath))
            {
                ZipArchive zip = new ZipArchive(zipStream);
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    if(entry.CompressedLength == 0)
                    {
                        continue;
                    }
                    string extractedFilePath = Path.Combine(expandedDirPath, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(extractedFilePath));
                    using (Stream zipFileStream = entry.Open())
                    {
                        using (FileStream extractedFileStream = File.OpenWrite(extractedFilePath))
                        {
                            await zipFileStream.CopyToAsync(extractedFileStream);
                        }
                    }
                }
            }
        }

        public async static Task UnGZip(string gzipPath, string expandedFilePath)
        {
            using (FileStream gzipStream = File.OpenRead(gzipPath))
            {
                using (GZipStream expandedStream = new GZipStream(gzipStream, CompressionMode.Decompress))
                {
                    using (FileStream targetFileStream = File.OpenWrite(expandedFilePath))
                    {
                        await expandedStream.CopyToAsync(targetFileStream);
                    }
                }
            }
        }

        public static void DirectoryCopy(string sourceDir, string destDir, bool overwrite = true)
        {
            
            DirectoryInfo dir = new DirectoryInfo(sourceDir);

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDir, file.Name);
                file.CopyTo(temppath, overwrite);
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDir, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, overwrite);
            }
        }

        public static void DeleteDirectory(string path)
        {
            int retries = 10;
            for(int i = 0; i < retries; i++)
            {
                if(!Directory.Exists(path))
                {
                    return;
                }
                try
                {
                    // On some systems, directories/files created programmatically are created with attributes
                    // that prevent them from being deleted. Set those attributes to be normal
                    SetAttributesNormal(path);
                    Directory.Delete(path, true);
                    return;
                }
                catch(IOException) when (i < retries-1)
                {
                }
                catch(UnauthorizedAccessException) when (i < retries - 1)
                {
                }
                // if something has a transient lock on the file waiting may resolve the issue
                Thread.Sleep((i+1) * 10);
            }
        }

        public static void SetAttributesNormal(string path)
        {
            foreach (var subDir in Directory.GetDirectories(path))
            {
                SetAttributesNormal(subDir);
            }
            foreach (var file in Directory.GetFiles(path))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
        }

        public static void MoveDirectory(string sourceDirName, string destDirName)
        {
            int retries = 10;
            for (int i = 0; i < retries; i++)
            {
                if (!Directory.Exists(sourceDirName) && Directory.Exists(destDirName))
                {
                    return;
                }
                try
                {
                    Directory.Move(sourceDirName, destDirName);
                    return;
                }
                catch (IOException) when (i < retries - 1)
                {
                }
                catch (UnauthorizedAccessException) when (i < retries - 1)
                {
                }
                // if something has a transient lock on the file waiting may resolve the issue
                Thread.Sleep((i + 1) * 10);
            }
        }

        public static void MoveFile(string sourceFileName, string destFileName)
        {
            int retries = 10;
            for (int i = 0; i < retries; i++)
            {
                if (!File.Exists(sourceFileName) && File.Exists(destFileName))
                {
                    return;
                }
                try
                {
                    File.Move(sourceFileName, destFileName);
                    return;
                }
                catch (IOException) when (i < retries - 1)
                {
                }
                catch (UnauthorizedAccessException) when (i < retries - 1)
                {
                }
                // if something has a transient lock on the file waiting may resolve the issue
                Thread.Sleep((i + 1) * 10);
            }
        }

        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
