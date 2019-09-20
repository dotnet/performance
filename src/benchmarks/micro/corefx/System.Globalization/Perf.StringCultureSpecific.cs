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
    public class Perf_StringCultureSpecific
    {
        private string _value, _same, _diffAtFirstChar, _diffAtLastChar, _firstHalf, _secondHalf;

        public static IEnumerable<(CultureInfo CultureInfo, CompareOptions CompareOptions)> GetCultureOptions()
        {
            // Ordinal and OrdinalIgnoreCase use single execution path for all cultures, so we test it only for "en-US"
            yield return (new CultureInfo("en-US"), CompareOptions.Ordinal);
            yield return (new CultureInfo("en-US"), CompareOptions.OrdinalIgnoreCase);

            yield return (new CultureInfo("en-US"), CompareOptions.None);
            yield return (new CultureInfo("en-US"), CompareOptions.IgnoreCase);
            yield return (new CultureInfo("en-US"), CompareOptions.IgnoreSymbols);

            yield return (CultureInfo.InvariantCulture, CompareOptions.None);
            yield return (CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);

            // both en-US and Invariant cultures have an optimized execution path on Unix systems:
            // https://github.com/dotnet/coreclr/blob/cd6bc26bdc4ac06fe2165b283eaf9fb5ff5293f4/src/System.Private.CoreLib/shared/System/Globalization/CompareInfo.Unix.cs#L35

            // so we need one more culture to test the "slow path"
            // Polish language has a lot of special characters, for example 'ch', 'rz', 'sz', 'cz' use two chars to express one ;)
            // it also has a lot of characters with accent so we use it as an example of "complex" language
            yield return (new CultureInfo("pl-PL"), CompareOptions.None);
        }

        [ParamsSource(nameof(GetCultureOptions))]
        public (CultureInfo CultureInfo, CompareOptions CompareOptions) CultureOptions;

        [Params(
            128, // small input that fits into stack-allocated array
            1024 * 128)] // medium size input that fits into an array rented from ArrayPool.Shared without allocation
        public int Count { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // we are using part of Alice's Adventures in Wonderland text as test data
            // it contains mostly simply ascii characters, but also some "high" chars that get special treatment and hit the slow path
            char[] characters = File.ReadAllText(CompressedFile.GetFilePath("alice29.txt")).ToCharArray().Take(Count).ToArray();
            _value = new string(characters);
            _same = new string(characters);
            _firstHalf = new string(characters.Take(Count / 2).ToArray());
            _secondHalf = new string(characters.Skip(Count / 2).ToArray());
            char[] copy = characters.ToArray();
            copy[0] = (char)(copy[0] + 1);
            _diffAtFirstChar = new string(copy);
            copy = characters.ToArray();
            copy[Count - 1] = (char)(copy[Count - 1] + 1);
            _diffAtLastChar = new string(copy);
        }

        [Benchmark]
        public new void GetHashCode() => CultureOptions.CultureInfo.CompareInfo.GetHashCode(_value, CultureOptions.CompareOptions);

        [Benchmark] // the most work to do: the strings have same conent, but don't point to the same memory
        public int Compare_Same() => CultureOptions.CultureInfo.CompareInfo.Compare(_value, _same, CultureOptions.CompareOptions);

        [Benchmark] // this should return quickly
        public int Compare_DifferentFirstChar() => CultureOptions.CultureInfo.CompareInfo.Compare(_value, _diffAtFirstChar, CultureOptions.CompareOptions);

        [Benchmark]
        public bool IsPrefix_FirstHalf() => CultureOptions.CultureInfo.CompareInfo.IsPrefix(_value, _firstHalf, CultureOptions.CompareOptions);

        [Benchmark] // this should return quickly
        public bool IsPrefix_DifferentFirstChar() => CultureOptions.CultureInfo.CompareInfo.IsPrefix(_value, _diffAtFirstChar, CultureOptions.CompareOptions);

        [Benchmark]
        public bool IsSuffix_SecondHalf() => CultureOptions.CultureInfo.CompareInfo.IsSuffix(_value, _secondHalf, CultureOptions.CompareOptions);

        [Benchmark] // this should return quickly
        public bool IsSuffix_DifferentLastChar() => CultureOptions.CultureInfo.CompareInfo.IsSuffix(_value, _diffAtLastChar, CultureOptions.CompareOptions);

        [Benchmark]
        public int IndexOf_SecondHalf() => CultureOptions.CultureInfo.CompareInfo.IndexOf(_value, _secondHalf, CultureOptions.CompareOptions);

        [Benchmark]
        public int LastIndexOf_FirstHalf() => CultureOptions.CultureInfo.CompareInfo.LastIndexOf(_value, _firstHalf, CultureOptions.CompareOptions);
    }
}
