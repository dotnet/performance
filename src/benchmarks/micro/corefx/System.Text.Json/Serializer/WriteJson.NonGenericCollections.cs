// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Tests
{
    public class WritesonNonGenericCollections
    {
        private static readonly IList _ilist = new List<string>();
        private static IEnumerable _ienumerable = new List<string>();
        private static ICollection _icollection = new List<string>();

        [Params(2, 50, 100)]
        public int ElementCount;

        [GlobalSetup]
        public void Setup()
        {
            for (int i = 0; i < ElementCount; i++)
            {
                _ilist.Add($"hello{i}");
            }

            _ienumerable = _ilist;
            _icollection = _ilist;
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public byte[] SerializeIList_ToUtf8Bytes()
        {
            return JsonSerializer.ToUtf8Bytes(_ilist);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public byte[] SerializeIEnumerable_ToUtf8Bytes()
        {
            return JsonSerializer.ToUtf8Bytes(_ienumerable);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public byte[] SerializeICollection_ToUtf8Bytes()
        {
            return JsonSerializer.ToUtf8Bytes(_icollection);
        }
    }
}
