// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Buffers.Tests
{
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.Span)]
    public class SearchValuesByteTests
    {
        private const int InputLength = 256;


        private SearchValues<byte> _searchValues;
        private byte[] _text;
        private byte[] _textExcept;

        [Params(
            "abcdefABCDEF0123456789",   // ASCII
            "abcdefABCDEF0123456789Ü"   // Mixed ASCII and non-ASCII
            )]
        public string Values;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private byte CharNotInSet() => (byte)'\n';

        [GlobalSetup]
        public void Setup()
        {
            _searchValues = SearchValues.Create(Encoding.Latin1.GetBytes(Values));

            byte charInSet = (byte)Values[0];

            _text = Enumerable.Repeat(CharNotInSet(), InputLength).ToArray();
            _textExcept = Enumerable.Repeat(charInSet, InputLength).ToArray();

            _text[InputLength / 2] = charInSet;
            _textExcept[InputLength / 2] = CharNotInSet();
        }

        [Benchmark]
        public bool Contains() => _searchValues.Contains(CharNotInSet());

        [Benchmark]
        public bool ContainsAny() => _text.AsSpan().ContainsAny(_searchValues);

        [Benchmark]
        public int IndexOfAny() => _text.AsSpan().IndexOfAny(_searchValues);

        [Benchmark]
        public int IndexOfAnyExcept() => _textExcept.AsSpan().IndexOfAnyExcept(_searchValues);
    }
}
