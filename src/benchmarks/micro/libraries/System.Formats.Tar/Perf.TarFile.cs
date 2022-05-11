// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Formats.Tar.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_TarFile
    {
        /*
        Always exist:
            root/
                inputdir/
                        testdir/
                        file.txt

        Created on demand:
                outputdir/
                output.tar
        */
        private readonly string _rootDirPath = FileUtils.GetTestFilePath();
        private readonly string _tarOutputPath = Path.Combine(_rootDirPath, "output.tar");
        private readonly string _inputDirPath = Path.Combine(_rootDirPath, "inputdir");
        private readonly string _testFilePath = Path.Combine(_inputDirPath, "file.txt");
        private readonly string _testDirPath = Path.Combine(_inputDirPath, "testdir");
        private readonly string _outputDirPath = Path.Combine(_rootDirPath, "outputdir");
        private MemoryStream _memoryStream;


        // Global setup and cleanup

        [GlobalSetup]
        public void SetupPaths()
        {
            _memoryStream = null;
            Directory.CreateDirectory(_rootTestPath);
            Directory.CreateDirectory(_inputDirPath);
            Directory.CreateDirectory(_testDirPath);
            File.Create(_testFilePath).Dispose();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            Directory.Delete(_rootTestPath, recursive: true);
            CleanupMemoryStream();
        }


        // TarFile.CreateFromDirectory(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory)

        [GlobalSetup(Target = nameof(TarFile_CreateFromDirectory_Path))]
        public void Setup_TarFile_CreateFromDirectory_Path() => File.Delete(_tarOutputPath);

        [GlobalCleanup(Target = nameof(TarFile_CreateFromDirectory_Path))]
        public void Cleanup_TarFile_CreateFromDirectory_Path() => File.Delete(_tarOutputPath);

        [Benchmark]
        public void TarFile_CreateFromDirectory_Path() => TarFile.CreateFromDirectory(sourceDirectoryName: _inputDirPath, destinationFileName: _tarOutputPath, includeBaseDirectory: false);


        // TarFile.CreateFromDirectory(string sourceDirectoryName, Stream destination, bool includeBaseDirectory)

        [GlobalSetup(Target = nameof(TarFile_CreateFromDirectory_Stream))]
        public void Setup_TarFile_CreateFromDirectory_Stream()
        {
            CleanupMemoryStream();
            _memoryStream = new MemoryStream();
        }

        [GlobalCleanup(Target = nameof(TarFile_CreateFromDirectory_Stream))]
        public void Cleanup_TarFile_CreateFromDirectory_Stream() => CleanupMemoryStream();

        [Benchmark]
        public void TarFile_CreateFromDirectory_Stream() => TarFile.CreateFromDirectory(sourceDirectoryName: _inputDirPath, destination: _memoryStream, includeBaseDirectory: false);


        // TarFile.ExtractToDirectory(string sourceFileName, string destinationDirectoryName, bool overwrite)

        [GlobalSetup(Target = nameof(TarFile_ExtractToDirectory_Path))]
        public void Setup_TarFile_ExtractToDirectory_Path()
        {
            File.Delete(_tarOutputPath);
            Directory.Delete(_outputDirPath);
            Directory.CreateDirectory(_outputDirPath);
            TarFile.CreateFromDirectory(sourceDirectoryName: _inputDirPath, destinationFileName: _tarOutputPath, includeBaseDirectory: false);
        }
        
        [GlobalCleanup(Target = nameof(TarFile_ExtractToDirectory_Path))]
        public void Cleanup_TarFile_ExtractToDirectory_Path()
        {
            File.Delete(_tarOutputPath);
            Directory.Delete(_outputDirPath);
        }

        [Benchmark]
        public void TarFile_ExtractToDirectory_Path() => TarFile.ExtractToDirectory(sourceFileName: _tarOutputPath, destinationDirectoryName: _outputDirPath, overwrite: false);


        // TarFile.ExtractToDirectory(Stream source, string destinationDirectoryName, bool overwrite)

        [GlobalSetup(Target = nameof(TarFile_ExtractToDirectory_Stream))]
        public void Setup_TarFile_ExtractToDirectory_Stream()
        {
            CleanupMemoryStream();
            TarFile.CreateFromDirectory(sourceDirectoryName: _inputDirPath, destination: _memoryStream, includeBaseDirectory: false);
        }

        [GlobalCleanup(Target = nameof(TarFile_ExtractToDirectory_Stream))]
        public void Cleanup_TarFile_ExtractToDirectory_Stream() => CleanupMemoryStream();

        [Benchmark]
        public void TarFile_ExtractToDirectory_Stream() => TarFile.ExtractToDirectory(source: _memoryStream, destinationDirectoryName: _outputDirPath, overwrite: false);


        // Helpers

        private void CleanupMemoryStream()
        {
            if (_memoryStream != null)
            {
                _memoryStream.Dispose();
                _memoryStream = null;
            }
        }
    }
}
