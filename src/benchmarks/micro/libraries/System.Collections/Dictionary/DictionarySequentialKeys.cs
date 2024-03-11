// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    public class DictionarySequentialKeys
    {
        private const int Seventeen = 17;
        private const int ThreeThousand = 3_000;

        private Dictionary<int, int> _dict_3k;
        private Dictionary<int, (long, long, long, long)> _dict32ByteValue_3k;
        private Dictionary<int, (object, object, object, object)> _dict32ByteRefsValue_3k;

        private Dictionary<int, int> _dict_17;
        private Dictionary<int, (long, long, long, long)> _dict32ByteValue_17;
        private Dictionary<int, (object, object, object, object)> _dict32ByteRefsValue_17;

        [GlobalSetup]
        public void Initialize()
        {
            (object, object, object, object) item = (new object(), new object(), new object(), new object());

            _dict_17 = Enumerable.Range(0, Seventeen).ToDictionary(i => i);
            _dict32ByteValue_17 = Enumerable.Range(0, Seventeen).ToDictionary(i => i, i => default((long, long, long, long)));
            _dict32ByteRefsValue_17 = Enumerable.Range(0, Seventeen).ToDictionary(i => i, i => item);

            _dict_3k = Enumerable.Range(0, ThreeThousand).ToDictionary(i => i);
            _dict32ByteValue_3k = Enumerable.Range(0, ThreeThousand).ToDictionary(i => i, i => default((long, long, long, long)));
            _dict32ByteRefsValue_3k = Enumerable.Range(0, ThreeThousand).ToDictionary(i => i, i => item);
        }

        [Benchmark(OperationsPerInvoke = Seventeen)]
        public int ContainsValue_17_Int_Int()
            => ContainsKey_Int_Int(_dict_17, Seventeen);

        [Benchmark(OperationsPerInvoke = Seventeen)]
        public int ContainsKey_17_Int_32ByteValue()
            => ContainsKey_Int_LargeStruct(_dict32ByteValue_17, Seventeen);

        [Benchmark(OperationsPerInvoke = Seventeen)]
        public int ContainsKey_17_Int_32ByteRefsValue() 
            => ContainsKey_Int_LargeRefStruct(_dict32ByteRefsValue_17, Seventeen);

        [Benchmark(OperationsPerInvoke = ThreeThousand)]
        public int ContainsValue_3k_Int_Int() 
            => ContainsKey_Int_Int(_dict_3k, ThreeThousand);

        [Benchmark(OperationsPerInvoke = ThreeThousand)]
        public int ContainsKey_3k_Int_32ByteValue()
            => ContainsKey_Int_LargeStruct(_dict32ByteValue_3k, ThreeThousand);

        [Benchmark(OperationsPerInvoke = ThreeThousand)]
        public int ContainsKey_3k_Int_32ByteRefsValue()
            => ContainsKey_Int_LargeRefStruct(_dict32ByteRefsValue_3k, ThreeThousand);

        private static int ContainsKey_Int_Int(Dictionary<int, int> d, int count)
        {
            int total = 0;

            for (int i = 0; i < count; i++)
            {
                if (d.ContainsKey(i))
                {
                    total++;
                }
            }

            return total;
        }

        private static int ContainsKey_Int_LargeStruct(Dictionary<int, (long, long, long, long)> d, int count)
        {
            int total = 0;

            for (int i = 0; i < count; i++)
            {
                if (d.ContainsKey(i))
                {
                    total++;
                }
            }

            return total;
        }

        private static int ContainsKey_Int_LargeRefStruct(Dictionary<int, (object, object, object, object)> d, int count)
        {
            int total = 0;

            for (int i = 0; i < count; i++)
            {
                if (d.ContainsKey(i))
                {
                    total++;
                }
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Seventeen)]
        [MemoryRandomization]
        public int TryGetValue_17_Int_Int()
            => TryGetValue_Int_Int(_dict_17, Seventeen);

        [Benchmark(OperationsPerInvoke = Seventeen)]
        public int TryGetValue_17_Int_32ByteValue()
            => TryGetValue_Int_LargeStruct(_dict32ByteValue_17, Seventeen);

        [Benchmark(OperationsPerInvoke = Seventeen)]
        public int TryGetValue_17_Int_32ByteRefsValue()
            => TryGetValue_Int_LargeRefStruct(_dict32ByteRefsValue_17, Seventeen);

        [Benchmark(OperationsPerInvoke = ThreeThousand)]
        public int TryGetValue_3k_Int_Int()
            => TryGetValue_Int_Int(_dict_3k, ThreeThousand);

        [Benchmark(OperationsPerInvoke = ThreeThousand)]
        public int TryGetValue_3k_Int_32ByteValue()
            => TryGetValue_Int_LargeStruct(_dict32ByteValue_3k, ThreeThousand);

        [Benchmark(OperationsPerInvoke = ThreeThousand)]
        public int TryGetValue_3k_Int_32ByteRefsValue()
            => TryGetValue_Int_LargeRefStruct(_dict32ByteRefsValue_3k, ThreeThousand);

        private static int TryGetValue_Int_Int(Dictionary<int, int> d, int count)
        {
            int total = 0;

            for (int i = 0; i < count; i++)
            {
                if (d.TryGetValue(i, out _))
                {
                    total++;
                }
            }

            return total;
        }

        private static int TryGetValue_Int_LargeStruct(Dictionary<int, (long, long, long, long)> d, int count)
        {
            int total = 0;

            for (int i = 0; i < count; i++)
            {
                if (d.TryGetValue(i, out _))
                {
                    total++;
                }
            }

            return total;
        }

        private static int TryGetValue_Int_LargeRefStruct(Dictionary<int, (object, object, object, object)> d, int count)
        {
            int total = 0;

            for (int i = 0; i < count; i++)
            {
                if (d.TryGetValue(i, out _))
                {
                    total++;
                }
            }

            return total;
        }
    }
}
