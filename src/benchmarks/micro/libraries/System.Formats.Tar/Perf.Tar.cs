// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Formats.Tar.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Perf_TarWriter
    {
        private static readonly string _fileName = "file.txt";
        private static Dictionary<string, string> _ea;
        private static MemoryStream _memoryStream;

        [GlobalSetup]
        public void Setup()
        {
            _memoryStream = new MemoryStream();
        }

        [GlobalSetup(Targets = new[] { nameof(PaxTarEntry_WriteEntry), nameof(PaxTarEntry_WriteEntry_Async) })]
        public void SetupPax()
        {
            _ea = new Dictionary<string, string>
            {
                { "uname", "username" },
                { "gname", "groupname" },
                { "uid", "483745" },
                { "gid", "193783" },
                { "mtime", "1409547224" },
                { "ctime", "1409547225" },
                { "atime", "1409547226" },
                // real world scenario:
                { "MSWINDOWS.rawsd", "AQAAgBQAAAAkAAAAAAAAAAAAAAABAgAAAAAABSAAAAAhAgAAAQIAAAAAAAUgAAAAIQIAAA==" }
            };
        }

        [Benchmark]
        public void V7TarEntry_WriteEntry()
        {
            V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, _fileName);
            using TarWriter writer = CreateWriter();
            writer.WriteEntry(entry);
        }

        [Benchmark]
        public async Task V7TarEntry_WriteEntry_Async()
        {
            V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, _fileName);
            await using TarWriter writer = CreateWriter();
            await writer.WriteEntryAsync(entry);
        }

        [Benchmark]
        public void UstarTarEntry_WriteEntry()
        {
            UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, _fileName);
            using TarWriter writer = CreateWriter();
            writer.WriteEntry(entry);
        }

        [Benchmark]
        public async Task UstarTarEntry_WriteEntry_Async()
        {
            UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, _fileName);
            await using TarWriter writer = CreateWriter();
            await writer.WriteEntryAsync(entry);
        }

        [Benchmark]
        public void PaxTarEntry_WriteEntry()
        {
            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, _fileName, _ea);
            using TarWriter writer = CreateWriter();
            writer.WriteEntry(entry);
        }

        [Benchmark]
        public async Task PaxTarEntry_WriteEntry_Async()
        {
            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, _fileName, _ea);
            await using TarWriter writer = CreateWriter();
            await writer.WriteEntryAsync(entry);
        }

        [Benchmark]
        public void GnuTarEntry_WriteEntry()
        {
            GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, _fileName);
            using TarWriter writer = CreateWriter();
            writer.WriteEntry(entry);
        }

        [Benchmark]
        public async Task GnuTarEntry_WriteEntry_Async()
        {
            GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, _fileName);
            await using TarWriter writer = CreateWriter();
            await writer.WriteEntryAsync(entry);
        }

        private TarWriter CreateWriter()
        {
            // BDN runs every benchmark more than once, so we want to reuse the memory stream instance
            // and have it always perform the same amount of work.
            _memoryStream.Position = 0;
            return new TarWriter(_memoryStream, leaveOpen: true);
        }
    }
}