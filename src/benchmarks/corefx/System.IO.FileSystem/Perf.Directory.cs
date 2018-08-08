// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace System.IO.Tests
{
    public class Perf_Directory
    {
        private readonly Consumer _consumer = new Consumer();
        private readonly string _testFile = FileUtils.GetTestFilePath();
        private readonly IReadOnlyDictionary<int, string> _testDeepFilePaths = new Dictionary<int, string>
        {
            { 10, GetTestDeepFilePath(10) },
            { 100, GetTestDeepFilePath(100) },
            { 1000, GetTestDeepFilePath(1000) }
        };
        private string[] _directoriesToCreate;

        [Benchmark]
        public void GetCurrentDirectory()
        {
            var consumer = _consumer;
            for (int i = 0; i < 20000; i++)
            {
                consumer.Consume(Directory.GetCurrentDirectory()); consumer.Consume(Directory.GetCurrentDirectory()); consumer.Consume(Directory.GetCurrentDirectory());
                consumer.Consume(Directory.GetCurrentDirectory()); consumer.Consume(Directory.GetCurrentDirectory()); consumer.Consume(Directory.GetCurrentDirectory());
                consumer.Consume(Directory.GetCurrentDirectory()); consumer.Consume(Directory.GetCurrentDirectory()); consumer.Consume(Directory.GetCurrentDirectory());
            }
        }
        
        [GlobalSetup(Target = nameof(CreateDirectory))]
        public void SetupCreateDirectory()
        {
            var testFile = FileUtils.GetTestFilePath();
            _directoriesToCreate = Enumerable.Range(1, 20000).Select(index => testFile + index).ToArray();
        }

        [Benchmark]
        public void CreateDirectory()
        {
            var directoriesToCreate = _directoriesToCreate;
            foreach (var directory in directoriesToCreate)
                Directory.CreateDirectory(directory);
        }
        
        [IterationCleanup(Target = nameof(CreateDirectory))]
        public void CleanupDirectoryIteration()
        {
            foreach (var directory in _directoriesToCreate)
                Directory.Delete(directory);
        }

        [GlobalSetup(Target = nameof(Exists))]
        public void SetupExists() => Directory.CreateDirectory(_testFile);

        [Benchmark]
        public bool Exists()
        {
            bool result = default;
            var testFile = _testFile;

            for (int i = 0; i < 20000; i++)
            {
                result ^= Directory.Exists(testFile); result ^= Directory.Exists(testFile); result ^= Directory.Exists(testFile);
                result ^= Directory.Exists(testFile); result ^= Directory.Exists(testFile); result ^= Directory.Exists(testFile);
                result ^= Directory.Exists(testFile); result ^= Directory.Exists(testFile); result ^= Directory.Exists(testFile);
            }

            return result;
        }
        
        [GlobalCleanup(Target = nameof(Exists))]
        public void CleanupExists() => Directory.Delete(_testFile);

        public IEnumerable<object[]> RecursiveDepthData()
        {
            yield return new object[] { 10, 100 };

            // Length of the path can be 260 characters on netfx.
            if (PathFeatures.AreAllLongPathsAvailable())
            {
                yield return new object[] { 100, 10 };
                // Most Unix distributions have a maximum path length of 1024 characters (1024 UTF-8 bytes). 
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    yield return new object[] { 1000, 1 };
            }
        }

        /// <remarks>Takes a lot of time to finish</remarks>
        [Benchmark]
        [ArgumentsSource(nameof(RecursiveDepthData))]
        public void RecursiveCreateDirectoryTest(int depth, int times)
        {
            string rootDirectory = _testFile;
            string path = _testDeepFilePaths[depth];

            for (int i = 0; i < times; i++)
                Directory.CreateDirectory(rootDirectory + Path.DirectorySeparatorChar + i + path);
        }
        
        [IterationCleanup(Target = nameof(RecursiveCreateDirectoryTest))]
        public void RecursiveCreateDirectoryTestIterationCleanup() => Directory.Delete(_testFile, recursive: true);

        /// <remarks>Takes a lot of time to finish</remarks>
        [Benchmark]
        [ArgumentsSource(nameof(RecursiveDepthData))]
        public void RecursiveDeleteDirectoryTest(int depth, int times)
        {
            string rootDirectory = _testFile;
            string path = _testDeepFilePaths[depth];

            for (int i = 0; i < times; i++)
            {
                Directory.CreateDirectory(rootDirectory + Path.DirectorySeparatorChar + i + path);
                Directory.Delete(rootDirectory + Path.DirectorySeparatorChar + i, recursive: true);
            }
        }
        
        [GlobalCleanup(Target = nameof(RecursiveDeleteDirectoryTest))]
        public void CleanupRecursiveDeleteDirectoryTest() => Directory.Delete(_testFile, recursive: true);

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
    }
}
