// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.DataMovement;
using Serilog;

namespace ArtifactsUploader
{
    public static class Uploader
    {
        public static async Task Upload(FileInfo archive, CommandLineOptions options, ILogger log, CancellationToken cancellationToken)
        {
            log.Information($"Starting the upload to {options.StorageUrl}");

            // following settings are recommended best practices https://github.com/Azure/azure-storage-net-data-movement#best-practice
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 8;
            ServicePointManager.Expect100Continue = false;

            var storageConnectionString = GetConnectionString(options);
            var account = CloudStorageAccount.Parse(storageConnectionString);
            var blobClient = account.CreateCloudBlobClient();

            var containerName = options.ProjectName; // we use project name (CoreFX/CoreCLR etc) as a container name
            var projectBlobContainer = blobClient.GetContainerReference(containerName);

            TransferManager.Configurations.ParallelOperations = 64; // value taken from https://github.com/Azure/azure-storage-net-data-movement

            var context = new SingleTransferContext
            {
                ProgressHandler = new Progress<TransferStatus>(progress => log.Information("Bytes uploaded: {0}", progress.BytesTransferred)),
            };

            var destinationBlob = projectBlobContainer.GetBlockBlobReference(GetDestinationBlobName(options, archive));

            await TransferManager.UploadAsync(archive.FullName, destinationBlob, null, context, cancellationToken);
        }

        private static string GetConnectionString(CommandLineOptions options)
            => $"BlobEndpoint={options.StorageUrl};SharedAccessSignature={GetSasToken(options)}";

        private static string GetSasToken(CommandLineOptions options)
            => options.SasToken ?? Environment.GetEnvironmentVariable("AZ_BLOB_LOGS_SAS_TOKEN");

        private static string GetDestinationBlobName(CommandLineOptions options, FileInfo archive)
            => Path.Combine(options.BranchName, archive.Name);
    }
}