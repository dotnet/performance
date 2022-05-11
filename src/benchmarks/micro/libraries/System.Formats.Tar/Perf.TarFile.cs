// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
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
        private static readonly string _rootDirPath = FileUtils.GetTestFilePath();
        private static readonly string _inputDirPath = Path.Combine(_rootDirPath, "inputdir");
        private static readonly string _outputDirPath = Path.Combine(_rootDirPath, "outputdir");
        private static readonly string _testDirPath = Path.Combine(_inputDirPath, "testdir");
        private static readonly string _testFilePath = Path.Combine(_inputDirPath, "file.txt");
        private static readonly string _inputTarFilePath = Path.Combine(_rootDirPath, "input.tar");
        private static readonly string _outputTarFilePath = Path.Combine(_rootDirPath, "output.tar");
        private MemoryStream _memoryStream = null;


        // Setup and Cleanup

        [GlobalSetup]
        public void Setup()
        {
            Directory.CreateDirectory(_rootDirPath);
            Directory.CreateDirectory(_inputDirPath);
            Directory.CreateDirectory(_testDirPath);
            File.Create(_testFilePath).Dispose();
            TarFile.CreateFromDirectory(sourceDirectoryName: _inputDirPath, destinationFileName: _inputTarFilePath, includeBaseDirectory: false);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            Directory.Delete(_rootDirPath, recursive: true);
            CleanupMemoryStream();
        }
        
        [IterationSetup]
        public void SetupIteration()
        {
            if (File.Exists(_outputTarFilePath))
            {
                File.Delete(_outputTarFilePath);
            }
            if (Directory.Exists(_outputDirPath))
            {
                Directory.Delete(_outputDirPath, recursive: true);
            }
            Directory.CreateDirectory(_outputDirPath);
            CleanupMemoryStream();
            _memoryStream = new MemoryStream();
        }

        [Benchmark]
        public void TarFile_CreateFromDirectory_Path() => TarFile.CreateFromDirectory(sourceDirectoryName: _inputDirPath, destinationFileName: _outputTarFilePath, includeBaseDirectory: false);

        [Benchmark]
        public void TarFile_CreateFromDirectory_Stream() => TarFile.CreateFromDirectory(sourceDirectoryName: _inputDirPath, destination: _memoryStream, includeBaseDirectory: false);

        [Benchmark]
        public void TarFile_ExtractToDirectory_Path() => TarFile.ExtractToDirectory(sourceFileName: _inputTarFilePath, destinationDirectoryName: _outputDirPath, overwriteFiles: false);

        [Benchmark]
        public void TarFile_ExtractToDirectory_Stream() => TarFile.ExtractToDirectory(source: _memoryStream, destinationDirectoryName: _outputDirPath, overwriteFiles: false);

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
