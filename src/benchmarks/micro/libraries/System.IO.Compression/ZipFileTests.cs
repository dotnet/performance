// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Compression;

[BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
public class ZipFileTests
{
    private static readonly string _testOutputDirectory = FileUtils.GetTestFilePath();
    private static readonly string _testOutputZipFile = Path.Combine(_testOutputDirectory, "TestOutputZipFile.zip");
    private static readonly string _testInputDirectory = Path.Combine(
        AppContext.BaseDirectory, "libraries", "System.IO.Compression", "TestData");
    private static readonly string _testInputZipFile = Path.Combine(
        AppContext.BaseDirectory, "libraries", "System.IO.Compression", "TestInputZipFile.zip");

    [IterationSetup(Target = nameof(ZipFileCreateFromDirectory))]
    public void IterationSetup()
    {
        Directory.CreateDirectory(_testOutputDirectory);
    }

    [IterationCleanup(Target = nameof(ZipFileCreateFromDirectory))]
    public void IterationCleanup()
    {
        Directory.Delete(_testOutputDirectory, recursive: true);
    }

    [Benchmark]
    public void ZipFileCreateFromDirectory()
    {
        ZipFile.CreateFromDirectory(_testInputDirectory, _testOutputZipFile);
    }

    public void ZipFileExtractToDirectory()
    {
        ZipFile.ExtractToDirectory(_testInputZipFile, _testOutputDirectory);
    }
}