// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Tests
{
    public class ReadGenericCollection
    {
        private static string _jsonString;

        [Params(2, 50, 100)]
        public int ElementCount;

        [GlobalSetup]
        public void Setup()
        {
            ISet<string> _iset = new HashSet<string>();
            for (int i = 0; i < ElementCount; i++)
            {
                _iset.Add($"hello{i}");
            }

            _jsonString = JsonSerializer.ToString(_iset);
        }

        [Benchmark]
        public ISet<string> DeserializeISet()
        {
            return JsonSerializer.Parse<ISet<string>>(_jsonString);
        }
    }
}