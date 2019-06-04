// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Tests
{
    public class WriteGenericCollection
    {
        private static readonly ISet<string> _iset = new HashSet<string>();

        [Params(2, 50, 100)]
        public int ElementCount;

        [GlobalSetup]
        public void Setup()
        {
            for (int i = 0; i < ElementCount; i++)
            {
                _iset.Add($"hello{i}");
            }
        }

        [Benchmark]
        public string SerializeISet_ToBytes()
        {
            return JsonSerializer.ToString(_iset);
        }
    }
}