// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using Test.Cryptography;

namespace System.Formats.Cbor.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_CborReader
    {
        [Benchmark]
        [ArgumentsSource(nameof(ECDSaCosePublicKeys))]
        public void ReadCoseKey(ECDsaCosePublicKey publicKey)
        {
            var reader = new CborReader(publicKey.EncodedCoseKey, CborConformanceMode.Ctap2Canonical);
            reader.ReadECParametersAsCosePublicKey();
        }

        public IEnumerable<object> ECDSaCosePublicKeys() => ECDsaCosePublicKey.CreatePublicKeys();

        [Benchmark]
        [ArgumentsSource(nameof(CborEncodings))]
        public void SkipValue(CborEncoding encoding)
        {
            var reader = new CborReader(encoding.Payload, encoding.ConformanceMode);
            reader.SkipValue();
        }

        public IEnumerable<object> CborEncodings() => CborEncoding.GetEncodings();

        public class CborEncoding
        {
            public CborEncoding(string name, string hexPayload, CborConformanceMode conformanceMode = CborConformanceMode.Strict)
            {
                Name = name;
                Payload = hexPayload.HexToByteArray();
                ConformanceMode = conformanceMode;
            }

            public string Name { get; }
            public byte[] Payload { get; }
            public CborConformanceMode ConformanceMode { get; }

            public override string ToString() => (Name, ConformanceMode).ToString();

            public static IEnumerable<CborEncoding> GetEncodings()
            {
                yield return new CborEncoding("Integer", "1907E4");
                yield return new CborEncoding("Text String", "6B6C6F72656D20697073756D");
                yield return new CborEncoding("Byte String", "49010203040506070809");
                yield return new CborEncoding("Array", "9ff4f6faffc00000fb7ff0000000000000ff");
                yield return new CborEncoding("Map", "a5010002006161006162008261636000", CborConformanceMode.Lax);
                yield return new CborEncoding("Map", "a5010002006161006162008261636000", CborConformanceMode.Strict);
                yield return new CborEncoding("Map", "a5010002006161006162008261636000", CborConformanceMode.Canonical);
            }
        }
    }
}
