// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace System.Globalization.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.CoreCLR)]
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
            if (ContainsHighChar(_value) != Options.highChars)
                throw new Exception(nameof(_value));
            if (ContainsHighChar(_diffAtFirstChar) != Options.highChars)
                throw new Exception(nameof(_diffAtFirstChar));
            if (ContainsHighChar(_diffAtLastChar) != Options.highChars)
                throw new Exception(nameof(_diffAtLastChar));
            if (Options.CultureInfo.CompareInfo.IsPrefix(new string(_value.First(), 1), new string(_diffAtFirstChar.First(), 1), Options.CompareOptions))
                throw new Exception(nameof(_diffAtFirstChar));
            if (Options.CultureInfo.CompareInfo.IsSuffix(new string(_value.Last(), 1), new string(_diffAtLastChar.Last(), 1), Options.CompareOptions))
                throw new Exception(nameof(_diffAtLastChar));
        }

        [Benchmark]
        public bool IsPrefix_FirstHalf() => Options.CultureInfo.CompareInfo.IsPrefix(_value, _firstHalf, Options.CompareOptions);

        [Benchmark] // this should return quickly
        public bool IsPrefix_DifferentFirstChar() => Options.CultureInfo.CompareInfo.IsPrefix(_value, _diffAtFirstChar, Options.CompareOptions);

        [Benchmark]
        public bool IsSuffix_SecondHalf() => Options.CultureInfo.CompareInfo.IsSuffix(_value, _secondHalf, Options.CompareOptions);

        [Benchmark] // this should return quickly
        public bool IsSuffix_DifferentLastChar() => Options.CultureInfo.CompareInfo.IsSuffix(_value, _diffAtLastChar, Options.CompareOptions);

        [Benchmark]
        public int IndexOf_Word_NotFound() => Options.CultureInfo.CompareInfo.IndexOf(_value, "word", Options.CompareOptions);

        [Benchmark]
        public int LastIndexOf_Word_NotFound() => Options.CultureInfo.CompareInfo.LastIndexOf(_value, "word", Options.CompareOptions);

        private static bool ContainsHighChar(string text) => text.Any(c => c > 0x80 || HighChars[c]);

        // source: https://github.com/dotnet/coreclr/blob/68ff240063fc2ddb9b03275ae5d5063a09d38ace/src/utilcode/util_nodependencies.cpp#L856-L993
        private static readonly bool[] HighChars = new bool[] {
            true, /* 0x0, 0x0 */
            true, /* 0x1, .*/
            true, /* 0x2, .*/
            true, /* 0x3, .*/
            true, /* 0x4, .*/
            true, /* 0x5, .*/
            true, /* 0x6, .*/
            true, /* 0x7, .*/
            true, /* 0x8, .*/
            false, /* 0x9,   */
            !RuntimeInformation.IsOSPlatform(OSPlatform.Windows), /* 0xA,  */
            false, /* 0xB, .*/
            false, /* 0xC, .*/
            !RuntimeInformation.IsOSPlatform(OSPlatform.Windows), /* 0xD,  */
            true, /* 0xE, .*/
            true, /* 0xF, .*/
            true, /* 0x10, .*/
            true, /* 0x11, .*/
            true, /* 0x12, .*/
            true, /* 0x13, .*/
            true, /* 0x14, .*/
            true, /* 0x15, .*/
            true, /* 0x16, .*/
            true, /* 0x17, .*/
            true, /* 0x18, .*/
            true, /* 0x19, .*/
            true, /* 0x1A, */
            true, /* 0x1B, .*/
            true, /* 0x1C, .*/
            true, /* 0x1D, .*/
            true, /* 0x1E, .*/
            true, /* 0x1F, .*/
            false, /*0x20,  */
            false, /*0x21, !*/
            false, /*0x22, "*/
            false, /*0x23,  #*/
            false, /*0x24,  $*/
            false, /*0x25,  %*/
            false, /*0x26,  &*/
            true,  /*0x27, '*/
            false, /*0x28, (*/
            false, /*0x29, )*/
            false, /*0x2A **/
            false, /*0x2B, +*/
            false, /*0x2C, ,*/
            true,  /*0x2D, -*/
            false, /*0x2E, .*/
            false, /*0x2F, /*/
            false, /*0x30, 0*/
            false, /*0x31, 1*/
            false, /*0x32, 2*/
            false, /*0x33, 3*/
            false, /*0x34, 4*/
            false, /*0x35, 5*/
            false, /*0x36, 6*/
            false, /*0x37, 7*/
            false, /*0x38, 8*/
            false, /*0x39, 9*/
            false, /*0x3A, :*/
            false, /*0x3B, ;*/
            false, /*0x3C, <*/
            false, /*0x3D, =*/
            false, /*0x3E, >*/
            false, /*0x3F, ?*/
            false, /*0x40, @*/
            false, /*0x41, A*/
            false, /*0x42, B*/
            false, /*0x43, C*/
            false, /*0x44, D*/
            false, /*0x45, E*/
            false, /*0x46, F*/
            false, /*0x47, G*/
            false, /*0x48, H*/
            false, /*0x49, I*/
            false, /*0x4A, J*/
            false, /*0x4B, K*/
            false, /*0x4C, L*/
            false, /*0x4D, M*/
            false, /*0x4E, N*/
            false, /*0x4F, O*/
            false, /*0x50, P*/
            false, /*0x51, Q*/
            false, /*0x52, R*/
            false, /*0x53, S*/
            false, /*0x54, T*/
            false, /*0x55, U*/
            false, /*0x56, V*/
            false, /*0x57, W*/
            false, /*0x58, X*/
            false, /*0x59, Y*/
            false, /*0x5A, Z*/
            false, /*0x5B, [*/
            false, /*0x5C, \*/
            false, /*0x5D, ]*/
            false, /*0x5E, ^*/
            false, /*0x5F, _*/
            false, /*0x60, `*/
            false, /*0x61, a*/
            false, /*0x62, b*/
            false, /*0x63, c*/
            false, /*0x64, d*/
            false, /*0x65, e*/
            false, /*0x66, f*/
            false, /*0x67, g*/
            false, /*0x68, h*/
            false, /*0x69, i*/
            false, /*0x6A, j*/
            false, /*0x6B, k*/
            false, /*0x6C, l*/
            false, /*0x6D, m*/
            false, /*0x6E, n*/
            false, /*0x6F, o*/
            false, /*0x70, p*/
            false, /*0x71, q*/
            false, /*0x72, r*/
            false, /*0x73, s*/
            false, /*0x74, t*/
            false, /*0x75, u*/
            false, /*0x76, v*/
            false, /*0x77, w*/
            false, /*0x78, x*/
            false, /*0x79, y*/
            false, /*0x7A, z*/
            false, /*0x7B, {*/
            false, /*0x7C, |*/
            false, /*0x7D, }*/
            false, /*0x7E, ~*/
            true, /*0x7F, */
        };
    }
}
