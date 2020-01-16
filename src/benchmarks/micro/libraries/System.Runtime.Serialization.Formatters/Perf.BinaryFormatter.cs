// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Runtime.Serialization.Formatters.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_BinaryFormatter
    {
        private readonly BinaryFormatter _formatter = new BinaryFormatter();
        private Stream _largeListStream;

        [GlobalSetup(Target = nameof(DeserializeLargeList))]
        public void LargeListSetup()
        {
            List<Book> list = Enumerable.Range(0, 100_000).Select(i =>
            {
                string id = i.ToString();
                return new Book { Name = id, Id = id };
            }).ToList();

            var mem = new MemoryStream();
            _formatter.Serialize(mem, list);
            _largeListStream = mem;
        }

        [Benchmark]
        public List<Book> DeserializeLargeList()
        {
            _largeListStream.Position = 0;
            return (List<Book>)_formatter.Deserialize(_largeListStream);
        }

        [Serializable]
        public class Book
        {
            public string Name;
            public string Id;
        }
    }
}
