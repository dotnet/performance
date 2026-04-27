// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Directory
    {
        private const int CreateInnerIterations = 10;
        
        private readonly string _testFile = FileUtils.GetTestFilePath();
        private readonly IReadOnlyDictionary<int, string> _testDeepFilePaths = new Dictionary<int, string>
        {
            { 10, GetTestDeepFilePath(10) },
            { 100, GetTestDeepFilePath(100) },
            { 1000, GetTestDeepFilePath(1000) }
        };
        private string[] _filePaths;
        private bool _flag;

        [Benchmark]
        [MemoryRandomization]
        public string GetCurrentDirectory() => Directory.GetCurrentDirectory();
        
        [GlobalSetup(Target = nameof(CreateDirectory))]
        public void SetupCreateDirectory()
        {
            var testFile = FileUtils.GetTestFilePath();
            _filePaths = Enumerable.Range(1, CreateInnerIterations).Select(index => testFile + index).ToArray();
        }

        [Benchmark(OperationsPerInvoke = CreateInnerIterations)]
        public void CreateDirectory()
        {
            var directoriesToCreate = _filePaths;
            foreach (var directory in directoriesToCreate)
                Directory.CreateDirectory(directory);
        }
        
        [IterationCleanup(Target = nameof(CreateDirectory))]
        public void CleanupDirectoryIteration()
        {
            foreach (var directory in _filePaths)
                Directory.Delete(directory, recursive: true);
        }

        [GlobalSetup(Target = nameof(Exists))]
        public void SetupExists() => Directory.CreateDirectory(_testFile);

        [Benchmark]
        public bool Exists() => Directory.Exists(_testFile);
        
        [GlobalCleanup(Target = nameof(Exists))]
        public void CleanupExists() => Directory.Delete(_testFile, recursive: true);

        public IEnumerable<object> RecursiveDepthData()
            => new object[] { 10, 100 } // Most Unix distributions have a maximum path length of 1024 characters (1024 UTF-8 bytes). 
                .Where(depth =>
                    {
                        try
                        {
                            var longPath = GetDirectoryPath(_testFile, (int)depth);

                            Directory.CreateDirectory(longPath);
                            Directory.Delete(_testFile, recursive: true);

                            return true;
                        }
                        catch (PathTooLongException)
                        {
                            return false;
                        }
                    }
                );

        [Benchmark]
        [ArgumentsSource(nameof(RecursiveDepthData))]
        public void RecursiveCreateDeleteDirectory(int depth)
        {
            var root = _testFile;
            var name = GetDirectoryPath(root, depth);

            Directory.CreateDirectory(name);
            Directory.Delete(root, recursive: true);
        }

        private string GetDirectoryPath(string root, int depth) => root + Path.DirectorySeparatorChar + _testDeepFilePaths[depth];

        private static string GetTestDeepFilePath(int depth)
        {
            string directory = Path.DirectorySeparatorChar + "a";
            StringBuilder sb = new StringBuilder(depth * 2);
            for (int i = 0; i < depth; i++)
            {
                sb.Append(directory);
            }

            return sb.ToString();
        }

        [GlobalSetup(Target = nameof(EnumerateFiles))]
        public void SetupEnumerateFiles()
        {
            Directory.CreateDirectory(_testFile);
            for (int i = 0; i < 10_000; i++)
            {
                File.Create(Path.Combine(_testFile, $"File{i}.txt")).Dispose();
            }
        }

        [Benchmark]
        public int EnumerateFiles() => Directory.EnumerateFiles(_testFile, "*", SearchOption.AllDirectories).Count();

        [GlobalCleanup(Target = nameof(EnumerateFiles))]
        public void CleanupEnumerateFiles() => Directory.Delete(_testFile, recursive: true);

        [GlobalSetup(Target = nameof(MoveFolders))]
        public void SetupMoveFolders() => SetupMove(true);

        [GlobalSetup(Target = nameof(MoveFiles))]
        public void SetupMoveFiles() => SetupMove(false);

        private void SetupMove(bool folders)
        {
            _filePaths = new[]
            {
                Path.Combine(Path.GetTempPath(), "foo_but_more_unique"),
                Path.Combine(Path.GetTempPath(), "bar_but_more_unique")
            };

            if (folders)
                Directory.CreateDirectory(_filePaths[0]);
            else
                File.Create(_filePaths[0]).Dispose();
        }

        [GlobalCleanup(Target = nameof(MoveFolders))]
        public void CleanupMoveFolders() => CleanupMove(true);

        [GlobalCleanup(Target = nameof(MoveFiles))]
        public void CleanupMoveFiles() => CleanupMove(false);

        private void CleanupMove(bool folders)
        {
            if (folders)
            {
                if (Directory.Exists(_filePaths[0])) Directory.Delete(_filePaths[0]);
                if (Directory.Exists(_filePaths[1])) Directory.Delete(_filePaths[1]);
            }
            else
            {
                if (File.Exists(_filePaths[0])) File.Delete(_filePaths[0]);
                if (File.Exists(_filePaths[1])) File.Delete(_filePaths[1]);
            }
        }

        [Benchmark]
        public void MoveFolders() => Move();

        [Benchmark]
        public void MoveFiles() => Move();

        private void Move()
        {
            _flag = !_flag;

            if (_flag)
            {
                Directory.Move(_filePaths[0], _filePaths[1]);
            }
            else
            {
                Directory.Move(_filePaths[1], _filePaths[0]);
            }
        }
    }
}
