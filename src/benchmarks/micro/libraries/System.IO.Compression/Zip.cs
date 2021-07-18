// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Linq;

namespace System.IO.Compression
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Zip
    {
        private static readonly string s_testFoldersLocation = Path.Combine(
            Path.GetDirectoryName(typeof(Zip).Assembly.Location), "libraries", "System.IO.Compression", "TestData");

        [Params("TestDocument", // 199 KB small test document with repeated paragraph, PDF are common.
            "alice29", // 145 KB, copy of "ALICE'S ADVENTURES IN WONDERLAND" book, an example of text file.
            "sum")] // 37.3 KB, some binary content, an example of binary file.
        public string TestFolder;

        [ParamsAllValues]
        public CompressionLevel CompressionLevel;

        private ZipArchive _archive;
        private ZipArchiveEntry _entry;
        private string _testFileLocation;
        private string _archiveLocation;
        // FileName at which the Zip archive entry is extracted to.
        private string _tempDestinationFileName;
        // Name of the entry that is created when the file is added into the Zip archive.
        private string _randomEntryName;


        [GlobalSetup(Target = nameof(ExtractEntryToFile))]
        public void SetupExtractTo()
        {
            string currentTestFolderLocation = Path.Combine(s_testFoldersLocation, TestFolder);
            _archiveLocation = currentTestFolderLocation + "zip";

            // Create a zip archive, then open it.
            // We will measure how much it takes to extract an entry using ExtractToFile.
            ZipFile.CreateFromDirectory(currentTestFolderLocation, _archiveLocation, CompressionLevel, includeBaseDirectory: false);
            _archive = ZipFile.OpenRead(_archiveLocation);
            _entry = _archive.Entries[0];
            _tempDestinationFileName = FileUtils.GetTestFilePath();
        }

        [GlobalSetup(Target = nameof(CreateEntryFromFile))]
        public void SetupCreateFrom()
        {
            string currentTestFolderLocation = Path.Combine(s_testFoldersLocation, TestFolder);
            _archiveLocation =  currentTestFolderLocation + ".zip";

            // Create zip archive and keep it open.
            // We will measure how much it takes to add an entry using CreateEntryFromFile.
            _archive = ZipFile.Open(_archiveLocation, ZipArchiveMode.Create);
            // Each folder in libraries\System.IO.Compression\TestData must contain only one file.
            _testFileLocation = Directory.EnumerateFiles(currentTestFolderLocation).First();
            _randomEntryName = Path.GetRandomFileName();
        }

        [Benchmark]
        public void ExtractEntryToFile() 
            => _entry.ExtractToFile(_tempDestinationFileName, overwrite: true);

        [Benchmark]
        public void CreateEntryFromFile() 
            => _archive.CreateEntryFromFile(_testFileLocation, _randomEntryName, CompressionLevel);

        [GlobalCleanup]
        public void Cleanup()
        {
            _archive.Dispose();
            File.Delete(_archiveLocation);
            if (_tempDestinationFileName != null)
            {
                File.Delete(_tempDestinationFileName);
            }
        }
    }
}
