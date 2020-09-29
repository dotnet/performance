// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;

namespace System.Formats.Cbor.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Perf_CborWriter
    {
        private CborWriter _ctap2Writer = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
        private byte[] _writeBuffer = new byte[1024];

        [Benchmark]
        [ArgumentsSource(nameof(ECDSaCosePublicKeys))]
        public void WriteCoseKey(ECDsaCosePublicKey publicKey)
        {
            _ctap2Writer.Reset();
            _ctap2Writer.WriteECParametersAsCosePublicKey(publicKey.ECParameters, publicKey.HashAlgorithmName);
            _ctap2Writer.Encode(_writeBuffer);
        }

        public IEnumerable<object> ECDSaCosePublicKeys() => ECDsaCosePublicKey.CreatePublicKeys();
    }
}
