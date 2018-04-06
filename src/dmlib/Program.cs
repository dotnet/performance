//------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation
// </copyright>
//------------------------------------------------------------------------------
﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Serialization;
#if !DOTNET5_4
using System.Runtime.Serialization.Formatters.Binary;
#endif
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.DataMovement;
using Microsoft.WindowsAzure.Storage.File;
using System.Linq;
using System.Text;
using System.Net;

namespace AzDataMovementBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            // Constant between tests
            ServicePointManager.Expect100Continue = false;
            TransferManager.Configurations.ParallelOperations = Environment.ProcessorCount * 8;
            
            // Upload blobs and ensure the report destination is clean
            PrepareBlobs();
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            if (File.Exists(reportPath))
            {
                File.Delete(reportPath);
            }
            
            // Download the blobs $ITERATIONS times and record the average throughput for each
            foreach (var i in Enumerable.Range(0, iterations))
            {
                var recorder = DownloadTest<HashStream>();
                File.AppendAllText(reportPath, $"Az Data Movement Library Benchmark,{recorder.AvgMbps:F2}\n");
            }
        }
        
        /// <summary>
        ///   Container name used in this sample.
        /// </summary>
        private static readonly string ContainerName = Environment.GetEnvironmentVariable("BENCHMARK_CONTAINER") ?? "benchmark-data";

        /// <summary>
        ///   Temporary folder used to store test data files
        /// </summary>
        private static readonly string TempFolderPath = Path.Combine(Path.GetTempPath(), "storage-benchmark");

        /// <summary>
        ///   The place to output a BenchView formatted report file
        /// </summary>
        private static readonly string reportPath = Environment.GetEnvironmentVariable("BV_REPORT_PATH") ?? Path.Combine("reports", "benchview.csv");

        /// <summary>
        ///   The size in bytes of the files to use for benchmarking
        /// </summary>
        private static readonly long fileSize = Int64.Parse(Environment.GetEnvironmentVariable("FILE_SIZE") ?? "104857600" /* 100MB */);
        
        /// <summary>
        ///   The number of files to transfer in the benchmark
        /// </summary>
        private static readonly int numFiles = Int32.Parse(Environment.GetEnvironmentVariable("NUM_FILES") ?? "50");
        
        /// <summary>
        ///   The number of transfer iterations
        /// </summary>
        private static readonly int iterations = Int32.Parse(Environment.GetEnvironmentVariable("ITERATIONS") ?? "20");

        private static void Record(string str) {
            if (reportPath != null) {
                File.AppendAllText(reportPath, str);
            }
        }

        /// <summary>
        /// Computes the hash of a stream and returns true if the given hash matches
        /// </summary>
        private static bool VerifyStream(Stream stream, byte[] hash)
        {
            var hashstream = stream as HashStream;
            if (hashstream != null)
            {
                return hashstream.Hash.SequenceEqual(hash);
            }
            var noopstream = stream as NoopStream;
            if (noopstream != null)
            {
                return true; // Just assume it worked
            }
            else {
                stream.Seek(0, SeekOrigin.Begin);

                using (var hasher = MD5.Create())
                {
                    return hasher.ComputeHash(stream).SequenceEqual(hash);
                }
            }
        }

        private static PerformanceRecorder DownloadTest<T>(bool useManager=true, bool useRecorder=true, bool useMD5Validation=false) where T : Stream, new()
        {
            var tasks = new List<Task<PerformanceRecorder>>();
            foreach (var n in Enumerable.Range(0, numFiles))
            {
                var blobName = $"{n}.dat";
                var hash = File.ReadAllBytes(Path.Combine(TempFolderPath, $"{n}-md5.dat"));
                tasks.Add(DownloadBlob<T>(blobName, hash, useManager: useManager, useRecorder: useRecorder, useMD5Validation: useMD5Validation));
            }

            PerformanceRecorder recorder = null;
            foreach (var result in Task.WhenAll(tasks).Result)
            {
                if (recorder == null) {
                    recorder = result;
                }
                else {
                    recorder.Merge(result);
                }
            }
            Console.WriteLine($"Total time was {recorder.Seconds} seconds at {recorder.AvgMbps} Mbps for {recorder.MBits * 1000 * 1000 / 1024 / 1024 / 8} MB");

            return recorder;
        }

        /// <summary>
        /// Download a file, verify the hash, and return a throughput record of the transfer
        /// </summary>
        private static async Task<PerformanceRecorder> DownloadBlob<T>(string blobName, byte[] hash, bool useManager=true, bool useRecorder=true, bool useMD5Validation=false) where T : Stream, new()
        {
            Console.WriteLine($"Starting download of {blobName}");
            // Create the source CloudBlob instances
            var blob = await Util.GetCloudBlobAsync(ContainerName, blobName, BlobType.BlockBlob);

            PerformanceRecorder recorder = null;

            using (var stream = new T())
            {
                recorder = new PerformanceRecorder(useRecorder ? stream : null);
                // Start the blob download
                recorder.Start();
                if (useManager) {
                    await TransferManager.DownloadAsync(blob, stream, new DownloadOptions{ DisableContentMD5Validation=!useMD5Validation, UseTransactionalMD5=false }, new SingleTransferContext());
                } else {
                    await blob.DownloadToStreamAsync(stream);
                }
                recorder.Stop();
                recorder.Bytes = stream.Length;

                if (VerifyStream(stream, hash))
                {
                    Console.WriteLine($"Downloaded {blobName}: Average speed {recorder.AvgMbps} Mbps");
                }
                else
                {
                    Console.WriteLine($"Failed to download {blobName}: Invalid hash");
                }
            }

            return recorder;
        }
        
        private static void PrepareBlobs()
        {
            var tasks = new List<Task<byte[]>>();
            foreach (var n in Enumerable.Range(0, numFiles))
            {
                var blobName = $"{n}.dat";
                tasks.Add(UploadRandomBlob(blobName, fileSize));
            }
            
            Directory.CreateDirectory(TempFolderPath);
            
            foreach (var n in Enumerable.Range(0, numFiles))
            {
                File.WriteAllBytes(Path.Combine(TempFolderPath, $"{n}-md5.dat"), tasks[n].Result);
            }
        }
        
        /// <summary>
        ///   Uploads a random data blob of the chosen size and yields the MD5 hash value
        ///   TODO: Store the hash value on write a verify the hash value instead of overwriting each time
        /// </summary>
        private static async Task<byte[]> UploadRandomBlob(string blobName, long size)
        {
            Console.WriteLine($"Starting upload of {blobName}");
            var blob = await Util.GetCloudBlobAsync(ContainerName, blobName, BlobType.BlockBlob);
            
            using (var stream = new RandomStream(size))
            {
                await TransferManager.UploadAsync(
                    stream,
                    blob,
                    new UploadOptions { },
                    new SingleTransferContext {
                        ShouldOverwriteCallback = (src, dst) => { return true; }
                    }
                );
                
                Console.WriteLine($"Done uploading of {blobName}");
                return stream.Hash;
            }
        }
    }
}
