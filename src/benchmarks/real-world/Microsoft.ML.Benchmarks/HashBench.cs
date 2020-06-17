// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.ML.Data;
using Microsoft.ML.Runtime;
using Microsoft.ML.Transforms;

namespace Microsoft.ML.Benchmarks
{
    /// <summary>
    /// These benchmarks measure the performance of the hashing function in ML.NET on both
    /// scalar and vector rows, for a variety of input types: string, float, double, key.
    /// The benchmarks initialize the hashing transformer with a 20 bit hash, then measure
    /// perf on fetching the first 100,000 values.
    /// </summary>
    [BenchmarkCategory(Categories.MachineLearning)]
    public class HashBench
    {
        private sealed class RowImpl : DataViewRow
        {
            public long PositionValue;

            public override DataViewSchema Schema { get; }
            public override long Position => PositionValue;
            public override long Batch => 0;
            public override ValueGetter<DataViewRowId> GetIdGetter()
                => (ref DataViewRowId val) => val = new DataViewRowId((ulong)Position, 0);

            private readonly Delegate _getter;

            /// <summary>
            /// Returns whether the given column is active in this row.
            /// </summary>
            public override bool IsColumnActive(DataViewSchema.Column column)
            {
                if (column.Index != 0)
                    throw new Exception();
                return true;
            }

            /// <summary>
            /// Returns a value getter delegate to fetch the valueof column with the given columnIndex, from the row.
            /// This throws if the column is not active in this row, or if the type
            /// <typeparamref name="TValue"/> differs from this column's type.
            /// </summary>
            /// <typeparam name="TValue"> is the output column's content type.</typeparam>
            /// <param name="column"> is the index of a output column whose getter should be returned.</param>
            public override ValueGetter<TValue> GetGetter<TValue>(DataViewSchema.Column column)
            {
                if (column.Index != 0)
                    throw new Exception();
                if (_getter is ValueGetter<TValue> typedGetter)
                    return typedGetter;
                throw new Exception();
            }

            public static RowImpl Create<T>(DataViewType type, ValueGetter<T> getter)
            {
                if (type.RawType != typeof(T))
                    throw new Exception();
                return new RowImpl(type, getter);
            }

            private RowImpl(DataViewType type, Delegate getter)
            {
                var builder = new DataViewSchema.Builder();
                builder.AddColumn("Foo", type, null);
                Schema = builder.ToSchema();
                _getter = getter;
            }
        }

        private const int Count = 100_000;

        private readonly MLContext _env = new MLContext();

        private RowImpl _inRow;
        private ValueGetter<uint> _getter;
        private ValueGetter<VBuffer<uint>> _vecGetter;

        private void InitMap<T>(T val, DataViewType type, int numberOfBits = 20, ValueGetter<T> getter = null)
        {
            if (getter == null)
                getter = (ref T dst) => dst = val;
            _inRow = RowImpl.Create(type, getter);
            // One million features is a nice, typical number.
            var info = _env.Transforms.Conversion.Hash("Bar", "Foo", numberOfBits: numberOfBits);
            var xf = info.Fit(new EmptyDataView(_inRow.Schema));
            var mapper = ((ITransformer)xf).GetRowToRowMapper(_inRow.Schema);
            var column = mapper.OutputSchema["Bar"];
            var outRow = mapper.GetRow(_inRow, new[] { column });
            if (type is VectorDataViewType)
                _vecGetter = outRow.GetGetter<VBuffer<uint>>(column);
            else
                _getter = outRow.GetGetter<uint>(column);
        }

        /// <summary>
        /// All the scalar mappers have the same output type.
        /// </summary>
        private void RunScalar()
        {
            uint val = default;
            for (int i = 0; i < Count; ++i)
            {
                _getter(ref val);
                ++_inRow.PositionValue;
            }
        }

        private void InitDenseVecMap<T>(T[] vals, PrimitiveDataViewType itemType, int numberOfBits = 20)
        {
            var vbuf = new VBuffer<T>(vals.Length, vals);
            InitMap(vbuf, new VectorDataViewType(itemType, vals.Length), numberOfBits, vbuf.CopyTo);
        }

        /// <summary>
        /// All the vector mappers have the same output type.
        /// </summary>
        private void RunVector()
        {
            VBuffer<uint> val = default;
            for (int i = 0; i < Count; ++i)
            {
                _vecGetter(ref val);
                ++_inRow.PositionValue;
            }
        }

        [GlobalSetup(Target = nameof(HashScalarString))]
        public void SetupHashScalarString() => InitMap("Hello".AsMemory(), TextDataViewType.Instance);

        [Benchmark]
        public void HashScalarString() => RunScalar();

        [GlobalSetup(Target = nameof(HashScalarFloat))]
        public void SetupHashScalarFloat() => InitMap(5.0f, NumberDataViewType.Single);

        [Benchmark]
        public void HashScalarFloat() => RunScalar();

        [GlobalSetup(Target = nameof(HashScalarDouble))]
        public void SetupHashScalarDouble() => InitMap(5.0, NumberDataViewType.Double);

        [Benchmark]
        public void HashScalarDouble() => RunScalar();

        [GlobalSetup(Target = nameof(HashScalarKey))]
        public void SetupHashScalarKey() => InitMap(6u, new KeyDataViewType(typeof(uint), 100));

        [Benchmark]
        public void HashScalarKey() => RunScalar();

        [GlobalSetup(Target = nameof(HashVectorString))]
        public void SetupHashVectorString()
        {
            var tokens = "Hello my friend, stay awhile and listen! ".Split().Select(token => token.AsMemory()).ToArray();
            InitDenseVecMap(tokens, TextDataViewType.Instance);
        }

        [Benchmark]
        public void HashVectorString() => RunVector();

        [GlobalSetup(Target = nameof(HashVectorFloat))]
        public void SetupHashVectorFloat() => InitDenseVecMap(new[] { 1f, 2f, 3f, 4f, 5f }, NumberDataViewType.Single);

        [Benchmark]
        public void HashVectorFloat() => RunVector();


        [GlobalSetup(Target = nameof(HashVectorDouble))]
        public void SetupHashVectorDouble() => InitDenseVecMap(new[] { 1d, 2d, 3d, 4d, 5d }, NumberDataViewType.Double);

        [Benchmark]
        public void HashVectorDouble() => RunVector();

        [GlobalSetup(Target = nameof(HashVectorKey))]
        public void SetupHashVectorKey() => InitDenseVecMap(new[] { 1u, 2u, 0u, 4u, 5u }, new KeyDataViewType(typeof(uint), 100));

        [Benchmark]
        public void HashVectorKey() => RunVector();

        /// <summary>
        /// An empty IDataView that has a schema, but no rows.
        /// </sumary>
        private sealed class EmptyDataView : IDataView
        {
            public bool CanShuffle => true;
            public DataViewSchema Schema { get; }

            public EmptyDataView(DataViewSchema schema)
            {
                Schema = schema;
            }

            public long? GetRowCount() => 0;

            public DataViewRowCursor GetRowCursor(IEnumerable<DataViewSchema.Column> columnsNeeded, Random rand = null)
            {
                return new Cursor(Schema);
            }

            public DataViewRowCursor[] GetRowCursorSet(IEnumerable<DataViewSchema.Column> columnsNeeded, int n, Random rand = null)
            {
                return new[] { new Cursor(Schema) };
            }

            private sealed class Cursor : DataViewRowCursor
            {
                public override DataViewSchema Schema { get; }
                public override long Batch => 0;

                public override long Position => -1;

                public Cursor(DataViewSchema schema)
                {
                    Schema = schema;
                }

                public override ValueGetter<DataViewRowId> GetIdGetter()
                {
                    return (ref DataViewRowId val) => throw new InvalidOperationException("No rows");
                }

                public override bool MoveNext() => false;

                public override bool IsColumnActive(DataViewSchema.Column column) => false;

                public override ValueGetter<TValue> GetGetter<TValue>(DataViewSchema.Column column)
                {
                    return (ref TValue value) => throw new Exception("No rows");
                }
            }
        }
    }
}
