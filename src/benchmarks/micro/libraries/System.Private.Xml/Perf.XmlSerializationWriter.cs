// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Xml.Serialization;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Xml.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_XmlSerializationWriter
    {
        private const int Iterations = 10000;

        private readonly MyXmlSerializationWriter _writer = new MyXmlSerializationWriter();

        private static readonly DateTime Now = new DateTime(2022, 9, 30, 9, 4, 15, DateTimeKind.Utc);
        private static readonly DateTimeOffset DtoNow = Now.AddDays(1);
        private static readonly TimeSpan Ts = new TimeSpan(1, 2, 3, 4, 5);
        private static readonly byte[] BArray = new byte[] { 33, 44, 55 };

        [IterationSetup]
        public void CleanWriter() => _writer.Clean();

        [Benchmark(OperationsPerInvoke = Iterations)]
        public int AddPrimitives()
        {
            for (int i = 0; i < Iterations; i++)
            {
                _writer.Write('a');
                _writer.Write(123);
                _writer.Write(123.45m);
                _writer.Write(Now);
                _writer.Write(Ts);
                _writer.Write(DtoNow);
                _writer.Write((short)55);
                _writer.Write(2345324L);
                _writer.Write((sbyte)11);
                _writer.Write((ushort)34);
                _writer.Write((uint)4564);
                _writer.Write((ulong)456734767);
                _writer.Write((byte)67);
                _writer.Write(BArray);
                _writer.Write(Guid.NewGuid());
            }

            return _writer.GetXmlLength();
        }
    }

    internal class MyXmlSerializationWriter : XmlSerializationWriter
    {
        private readonly StringBuilder _builder = new StringBuilder();

        public MyXmlSerializationWriter()
        {
            Writer = new XmlTextWriter(new StringWriter(_builder));
        }

        protected override void InitCallbacks()
        {
        }

        public void Write<T>(T value) => WriteTypedPrimitive(null, null, value, false);

        public void Clean() => _builder.Clear();
        public int GetXmlLength() => _builder.Length;
    }
}
