// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

#pragma warning disable CS0618 // obsolete

namespace System.Net.Primitives.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class IPAddressPerformanceTests
    {
        public static IEnumerable<object> ByteAddresses()
        {
            yield return new byte[] { 0x8f, 0x18, 0x14, 0x24 };
            yield return new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        }
        
        public IEnumerable<object> Addresses() 
            => ByteAddresses().OfType<byte[]>().Select(bytes => new IPAddress(bytes));

        private byte[] _destination = new byte[ByteAddresses().OfType<byte[]>().Max(bytes => bytes.Length)];
        private const int INET6_ADDRSTRLEN = 65;
        private char[] _charBuffer = new char[INET6_ADDRSTRLEN];

        [Benchmark]
        [ArgumentsSource(nameof(Addresses))]
        public byte[] GetAddressBytes(IPAddress address)
            => address.GetAddressBytes();

        [Benchmark]
        [ArgumentsSource(nameof(ByteAddresses))]
        [MemoryRandomization]
        public IPAddress Ctor_Bytes(byte[] address)
            => new IPAddress(address);

        [Benchmark]
        [ArgumentsSource(nameof(ByteAddresses))]
        public string CtorAndToString(byte[] address)
            => new IPAddress(address).ToString();

        private static readonly long s_addr = IPAddress.Loopback.Address;

#if !NETFRAMEWORK // API added in .NET Core 2.1
        [Benchmark]
        [ArgumentsSource(nameof(ByteAddresses))]
        public IPAddress Ctor_Span(byte[] address)
            => new IPAddress(new ReadOnlySpan<byte>(address));

        [Benchmark]
        [ArgumentsSource(nameof(Addresses))]
        public bool TryFormat(IPAddress address)
            => address.TryFormat(new Span<char>(_charBuffer), out _);
        
        [Benchmark]
        [ArgumentsSource(nameof(Addresses))]
        public bool TryWriteBytes(IPAddress address)
            => address.TryWriteBytes(new Span<byte>(_destination), out _);
#endif
    }
}
