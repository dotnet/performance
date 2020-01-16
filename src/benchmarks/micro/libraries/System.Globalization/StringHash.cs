// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace System.Globalization.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.CoreCLR)]
    public class StringHash
    {
        public static IEnumerable<(CultureInfo CultureInfo, CompareOptions CompareOptions)> GetOptions()
        {
            // Ordinal and OrdinalIgnoreCase use single execution path for all cultures, so we test it only for "en-US"
            yield return (new CultureInfo("en-US"), CompareOptions.Ordinal);
            yield return (new CultureInfo("en-US"), CompareOptions.OrdinalIgnoreCase);

            yield return (new CultureInfo("en-US"), CompareOptions.None);
            yield return (new CultureInfo("en-US"), CompareOptions.IgnoreCase);

            yield return (CultureInfo.InvariantCulture, CompareOptions.None);
            yield return (CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
        }

        [ParamsSource(nameof(GetOptions))]
        public (CultureInfo CultureInfo, CompareOptions CompareOptions) Options;

        [Params(
            128, // small input that fits into stack-allocated array https://github.com/dotnet/coreclr/blob/c6675ef2e22474d6222d054ae3d022c01eda9b6d/src/System.Private.CoreLib/shared/System/Globalization/CompareInfo.Unix.cs#L824
            1024 * 128)] // medium size input that fits into an array rented from ArrayPool.Shared without allocation
        public int Count;

        private string _value;

        [GlobalSetup] // we are using part of Alice's Adventures in Wonderland text as test data
        public void Setup() => _value = new string(File.ReadAllText(CompressedFile.GetFilePath("alice29.txt")).Take(Count).ToArray());

        [Benchmark]
        public new void GetHashCode() => Options.CultureInfo.CompareInfo.GetHashCode(_value, Options.CompareOptions);
    }
}
