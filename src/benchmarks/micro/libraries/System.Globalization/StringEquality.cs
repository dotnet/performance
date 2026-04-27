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
    [BenchmarkCategory(Categories.Libraries, Categories.Runtime, Categories.NoWASM)]
    public class StringEquality
    {
        private string _value, _same, _sameUpper, _diffAtFirstChar;

        public static IEnumerable<(CultureInfo CultureInfo, CompareOptions CompareOptions)> GetOptions()
        {
            // Ordinal and OrdinalIgnoreCase use single execution path for all cultures, so we test it only for "en-US"
            yield return (new CultureInfo("en-US"), CompareOptions.Ordinal);
            yield return (new CultureInfo("en-US"), CompareOptions.OrdinalIgnoreCase);

            // the most popular culture:
            yield return (new CultureInfo("en-US"), CompareOptions.None);
            yield return (new CultureInfo("en-US"), CompareOptions.IgnoreCase);

            // two very common use cases:
            yield return (CultureInfo.InvariantCulture, CompareOptions.None);
            yield return (CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);

            // IgnoreSymbols and IgnoreNonSpace are rarely used, this is why we test it only for a single culture
            yield return (new CultureInfo("en-US"), CompareOptions.IgnoreSymbols);
            yield return (new CultureInfo("en-US"), CompareOptions.IgnoreNonSpace);

            // Polish language has a lot of special characters, for example 'ch', 'rz', 'sz', 'cz' use two chars to express one ;)
            // it also has a lot of characters with accent so we use it as an example of a "complex" language
            yield return (new CultureInfo("pl-PL"), CompareOptions.None);
        }

        [ParamsSource(nameof(GetOptions))]
        public (CultureInfo CultureInfo, CompareOptions CompareOptions) Options;

        [Params(1024)] // single execution path = single test case
        public int Count;

        [GlobalSetup]
        public void Setup()
        {
            // we are using part of Alice's Adventures in Wonderland text as test data
            char[] characters = File.ReadAllText(CompressedFile.GetFilePath("alice29.txt")).Take(Count).ToArray();
            _value = new string(characters);
            _same = new string(characters);
            _sameUpper = _same.ToUpper();
            char[] copy = characters.ToArray();
            copy[0] = (char)(copy[0] + 1);
            _diffAtFirstChar = new string(copy);
        }

        [Benchmark] // the most work to do: the strings have same content, but don't point to the same memory
        [MemoryRandomization]
        public int Compare_Same() => Options.CultureInfo.CompareInfo.Compare(_value, _same, Options.CompareOptions);

        [Benchmark] // the most work to do for IgnoreCase: every char needs to be compared and uppercased
        [MemoryRandomization]
        public int Compare_Same_Upper() => Options.CultureInfo.CompareInfo.Compare(_value, _sameUpper, Options.CompareOptions);

        [Benchmark] // this should return quickly
        [MemoryRandomization]
        public int Compare_DifferentFirstChar() => Options.CultureInfo.CompareInfo.Compare(_value, _diffAtFirstChar, Options.CompareOptions);
    }
}
