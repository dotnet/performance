// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
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

            var storageConnectionString = GetConnectionString(options);
            var account = CloudStorageAccount.Parse(storageConnectionString);
            var blobClient = account.CreateCloudBlobClient();

            var containerName = options.ProjectName; // we use project name (CoreFX/CoreCLR etc) as a container name
            var projectBlobContainer = blobClient.GetContainerReference(containerName);
            var containerCreated = await projectBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null, cancellationToken);

            if (containerCreated)
            {
                log.Warning($"Container {containerName} did not exist before, was created now");
            }

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
            => options.SasToken ?? Environment.GetEnvironmentVariable("AZ_BLOB_LOGS_SAS_TOKEN"); // Jenkin secret manager plugin spawns the process with this env var configured;

        private static string GetDestinationBlobName(CommandLineOptions options, FileInfo archive)
            => archive.Name; // todo: is this enough?
    }
}