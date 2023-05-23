// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Formats.Tar.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Perf_TarFile
    {
        /*
        Always exist:
            root/
                inputdir/
                        testdir/
                        file.txt
                input.tar

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

        [GlobalSetup]
        public void Setup()
        {
            Directory.CreateDirectory(_testDirPath); // Creates all segments: root/inputdir/testdir
            Directory.CreateDirectory(_outputDirPath);
            File.Create(_testFilePath).Dispose();
            TarFile.CreateFromDirectory(sourceDirectoryName: _inputDirPath, destinationFileName: _inputTarFilePath, includeBaseDirectory: false);
        }

        [GlobalCleanup]
        public void Cleanup() => Directory.Delete(_rootDirPath, recursive: true);

        [Benchmark]
        public void CreateFromDirectory_Path()
        {
            TarFile.CreateFromDirectory(sourceDirectoryName: _inputDirPath, destinationFileName: _outputTarFilePath, includeBaseDirectory: false);
            File.Delete(_outputTarFilePath);
        }

        [Benchmark]
        public async Task CreateFromDirectory_Path_Async()
        {
            await TarFile.CreateFromDirectoryAsync(sourceDirectoryName: _inputDirPath, destinationFileName: _outputTarFilePath, includeBaseDirectory: false);
            File.Delete(_outputTarFilePath);
        }

        [Benchmark]
        public void ExtractToDirectory_Path() => TarFile.ExtractToDirectory(sourceFileName: _inputTarFilePath, destinationDirectoryName: _outputDirPath, overwriteFiles: true);

        [Benchmark]
        public Task ExtractToDirectory_Path_Async() => TarFile.ExtractToDirectoryAsync(sourceFileName: _inputTarFilePath, destinationDirectoryName: _outputDirPath, overwriteFiles: true);

        [Benchmark]
        public void CreateFromDirectory_Stream()
        {
            using MemoryStream ms = new MemoryStream();
            TarFile.CreateFromDirectory(sourceDirectoryName: _inputDirPath, destination: ms, includeBaseDirectory: false);
        }

        [Benchmark]
        public async Task CreateFromDirectory_Stream_Async()
        {
            await using MemoryStream ms = new MemoryStream();
            await TarFile.CreateFromDirectoryAsync(sourceDirectoryName: _inputDirPath, destination: ms, includeBaseDirectory: false);
        }

        [Benchmark]
        public void ExtractToDirectory_Stream()
        {
            using FileStream fs = File.OpenRead(_inputTarFilePath);
            TarFile.ExtractToDirectory(source: fs, destinationDirectoryName: _outputDirPath, overwriteFiles: true);
        }

        [Benchmark]
        public async Task ExtractToDirectory_Stream_Async()
        {
            await using FileStream fs = new FileStream(_inputTarFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            await TarFile.ExtractToDirectoryAsync(source: fs, destinationDirectoryName: _outputDirPath, overwriteFiles: true);
        }
    }
}
