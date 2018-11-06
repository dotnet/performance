// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using BenchmarkDotNet.Attributes;

namespace System
{
    public class Hashing
    {
        private string _string;

        [Params(10, 100, 1_000, 10_000)]
        public int BytesCount;

        /// <summary>
        /// Marvin is internal, this is why we call string.GetHashCode to measure it's performance
        /// see https://github.com/dotnet/corefx/blob/8252ecc2eb0da08cd474a303b646e111d74d2a71/src/Common/src/CoreLib/System/String.Comparison.cs#L749
        /// </summary>
        [GlobalSetup]
        public void Setup() => _string = new string(Enumerable.Repeat('a', BytesCount / (sizeof(char)/ sizeof(byte))).ToArray());

        [Benchmark]
        public int GetStringHashCode() => _string.GetHashCode();
    }
}