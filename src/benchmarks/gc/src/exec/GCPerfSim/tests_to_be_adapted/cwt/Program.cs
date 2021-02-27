// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices; // ConditionalWeakTable

namespace cwt
{
    class SomeId
    {
        //TODO
    }

    class SomeKey
    {
        private byte[] _data;
        private string _id;
        public SomeKey(string id, int dataSize)
        {
            this._data = new byte[dataSize];
            this._id = id;
        }
    }

    class SomeClass1
    {
        private int _data;
        public SomeClass1(int data)
        {
            this._data = data;
        }
    }

    class SomeSet
    {
        private byte[] _data;
        private string _id;
        private SomeClass1 _ref;

        public SomeSet(string id, int dataSize)
        {
            this._data = new byte[dataSize];
            this._id = id;
            this._ref = new SomeClass1(dataSize);
        }
    }

    class Program
    {
        private static readonly ConditionalWeakTable<SomeKey, SomeSet> s_cwt = new ConditionalWeakTable<SomeKey, SomeSet>();
        private static readonly ConditionalWeakTable<SomeKey, ConditionalWeakTable<SomeId, SomeSet>> s_cwt_cwt = new ConditionalWeakTable<SomeKey, ConditionalWeakTable<SomeId, SomeSet>>();

        static void Main2(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: ./cwttest.exe [bytes to allocate] [number of iterations]");
            }


            int bytesToAllocate = Int32.Parse(args[0]);
            int numberOfIterations = Int32.Parse(args[1]);

            int bytesAllocated = 0;

            const int objSize = 1500; // TODO: make this configurable


            for (var i = 0; i < numberOfIterations; i++)
            {

                while (bytesAllocated < bytesToAllocate)
                {

                    SomeKey sk = new SomeKey(i.ToString(), objSize);
                    SomeSet ss = new SomeSet(i.ToString(), objSize);
                    // s_cwt.AddOrUpdate(sk, ss); // Not available in .net 4.7.2
                    // s_cwt_cwt.AddOrUpdate(sk, ss); // This doesn't compile!
                    bytesAllocated += (objSize * 2);
                    throw new NotImplementedException(); // See commented-out code above
                }
            }
        }
    }
}
