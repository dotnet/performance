// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;

namespace System.Globalization.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.Runtime)]
    public class StringSearch
    {
        private string _value, _diffAtFirstChar, _diffAtLastChar, _firstHalf, _secondHalf;

        public static IEnumerable<(CultureInfo CultureInfo, CompareOptions CompareOptions, bool highChars)> GetOptions()
        {
            // Ordinal and OrdinalIgnoreCase use single execution path for all cultures, so we test it only for "en-US"
            // without enforcing highChars - the execution path would be the same
            yield return (new CultureInfo("en-US"), CompareOptions.Ordinal, false);
            yield return (new CultureInfo("en-US"), CompareOptions.OrdinalIgnoreCase, false);

            yield return (new CultureInfo("en-US"), CompareOptions.None, false); // no high chars = fast path (managed code on Unix)
            yield return (new CultureInfo("en-US"), CompareOptions.None, true); // high chars = slow path (call to ICU on Unix)
            yield return (new CultureInfo("en-US"), CompareOptions.IgnoreCase, false);
            yield return (new CultureInfo("en-US"), CompareOptions.IgnoreCase, true);

            // IgnoreSymbols always uses the slow path
            // https://github.com/dotnet/coreclr/blob/cd6bc26bdc4ac06fe2165b283eaf9fb5ff5293f4/src/System.Private.CoreLib/shared/System/Globalization/CompareInfo.Unix.cs#L911-L912
            yield return (new CultureInfo("en-US"), CompareOptions.IgnoreSymbols, false);

            // too rarely used to test both slow and fast path
            yield return (new CultureInfo("en-US"), CompareOptions.IgnoreNonSpace, false);

            yield return (CultureInfo.InvariantCulture, CompareOptions.None, false);
            yield return (CultureInfo.InvariantCulture, CompareOptions.None, true);
            yield return (CultureInfo.InvariantCulture, CompareOptions.IgnoreCase, false);
            yield return (CultureInfo.InvariantCulture, CompareOptions.IgnoreCase, true);

            // both en-US and Invariant cultures have an optimized execution path on Unix systems:
            // https://github.com/dotnet/coreclr/blob/cd6bc26bdc4ac06fe2165b283eaf9fb5ff5293f4/src/System.Private.CoreLib/shared/System/Globalization/CompareInfo.Unix.cs#L35
            // so we need one more culture to test the "slow path"
            // Polish language has a lot of special characters, for example 'ch', 'rz', 'sz', 'cz' use two chars to express one ;)
            // it also has a lot of characters with accent so we use it as an example of "complex" language
            yield return (new CultureInfo("pl-PL"), CompareOptions.None, false);
        }

        [ParamsSource(nameof(GetOptions))]
        public (CultureInfo CultureInfo, CompareOptions CompareOptions, bool highChars) Options;

        [GlobalSetup]
        public void Setup()
        {
            // we are using simple input to mimic "real world test case"
            char[] characters = "NET Conf provides a wide selection of live sessions streaming here that feature speakers from the community and .NET product teams. It is a chance to learn, ask questions live, and get inspired for your next software project".ToArray();

            if (!ContainsSimpleCharactersOnly(characters))
                throw new Exception("The sentence must contain only simple characters to ensure that it contains high chars only when Options.highChars is set to true");

            // we ensure that high chars are present by inserting one
            if (Options.highChars)
            {
                characters[0] = (char)0x81; // at the begning for IndexOf and StartsWith
                characters[characters.Length - 1] = (char)0x81; // at the begning for LastIndexOf and EndsWith
            }

            _value = new string(characters);
            _firstHalf = new string(characters.Take(characters.Length / 2).ToArray());
            _secondHalf = new string(characters.Skip(characters.Length / 2).ToArray());
            char[] copy = characters.ToArray();
            // to get a different char we can not just increment the first|last char because for the HighChars=true
            // CultureInfo.GetCultureInfo("en-US").CompareInfo.IsSuffix(new string((char)0x81, 1), new string((char)0x82, 1)) returns true
            copy[0] = (char)(Options.highChars ? copy[0] * 2 : copy[0] + 1);
            _diffAtFirstChar = new string(copy);
            copy = characters.ToArray();
            copy[characters.Length - 1] = (char)(Options.highChars ? copy[characters.Length - 1] * 2 : copy[characters.Length - 1] + 1);
            _diffAtLastChar = new string(copy);

            // now we need to ensure that the test data is correct to avoid issues like https://github.com/dotnet/performance/pull/909
            if (Options.CultureInfo.CompareInfo.IsPrefix(new string(_value.First(), 1), new string(_diffAtFirstChar.First(), 1), Options.CompareOptions))
                throw new Exception(nameof(_diffAtFirstChar));
            if (Options.CultureInfo.CompareInfo.IsSuffix(new string(_value.Last(), 1), new string(_diffAtLastChar.Last(), 1), Options.CompareOptions))
                throw new Exception(nameof(_diffAtLastChar));
        }

        [Benchmark]
        public bool IsPrefix_FirstHalf() => Options.CultureInfo.CompareInfo.IsPrefix(_value, _firstHalf, Options.CompareOptions);

        [Benchmark] // this should return quickly
        [MemoryRandomization]
        public bool IsPrefix_DifferentFirstChar() => Options.CultureInfo.CompareInfo.IsPrefix(_value, _diffAtFirstChar, Options.CompareOptions);

        [Benchmark]
        [MemoryRandomization]
        public bool IsSuffix_SecondHalf() => Options.CultureInfo.CompareInfo.IsSuffix(_value, _secondHalf, Options.CompareOptions);

        [Benchmark] // this should return quickly
        [MemoryRandomization]
        public bool IsSuffix_DifferentLastChar() => Options.CultureInfo.CompareInfo.IsSuffix(_value, _diffAtLastChar, Options.CompareOptions);

        [Benchmark]
        [MemoryRandomization]
        public int IndexOf_Word_NotFound() => Options.CultureInfo.CompareInfo.IndexOf(_value, "word", Options.CompareOptions);

        [Benchmark]
        [MemoryRandomization]
        public int LastIndexOf_Word_NotFound() => Options.CultureInfo.CompareInfo.LastIndexOf(_value, "word", Options.CompareOptions);

        private static bool ContainsSimpleCharactersOnly(char[] text) => text.All(c => c == ' ' || c == '.' || c == ',' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
    }
}
