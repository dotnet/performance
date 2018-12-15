// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_FileInfo
    {
        private readonly string _path = FileUtils.GetTestFilePath();

        [Benchmark]
        public FileInfo ctor_str() => new FileInfo(_path);
    }
}
