// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    public class CompressedFile
    {
        public string Name { get; }
        public CompressionLevel CompressionLevel { get; }

        public byte[] UncompressedData { get; }
        public byte[] CompressedData { get; }
        public MemoryStream CompressedDataStream { get; }
            
        public CompressedFile(string fileName, CompressionLevel compressionLevel, Func<Stream, CompressionLevel, Stream> factory)
        {
            Name = fileName;
            CompressionLevel = compressionLevel;

            var filePath = GetFilePath(fileName);
            UncompressedData = File.ReadAllBytes(filePath);
            CompressedDataStream = new MemoryStream(capacity: UncompressedData.Length);

            var compressionStream = factory(CompressedDataStream, compressionLevel);
            compressionStream.Write(UncompressedData, 0, UncompressedData.Length);
            compressionStream.Flush();
            
            CompressedDataStream.Position = 0;
            CompressedData = CompressedDataStream.ToArray();
        }

        public override string ToString() => Name;

        internal static string GetFilePath(string fileName) 
            => Path.Combine(
                Path.GetDirectoryName(typeof(CompressedFile).Assembly.Location), 
                "corefx", "System.IO.Compression", "TestData",
                fileName);
    }
}