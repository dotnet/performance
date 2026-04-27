using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace System.Collections
{
    public readonly struct NotKnownComparable : IEquatable<NotKnownComparable>
    {
        private readonly short _value;

        public NotKnownComparable(short value) => _value = value;

        public bool Equals(NotKnownComparable other) => _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public override bool Equals(object obj) => obj is NotKnownComparable other && Equals(other);
    }

    public sealed class ReferenceType : IEquatable<ReferenceType>
    {
        private readonly short _value;

        public ReferenceType(short value) => _value = value;

        public bool Equals(ReferenceType other) => _value == other._value;

        public override bool Equals(object obj) => obj is ReferenceType other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();
    }

    [BenchmarkCategory(Categories.Libraries)]
    [GenericTypeArguments(typeof(short))] // int has special treatment
    [GenericTypeArguments(typeof(NotKnownComparable))] // value types from pre-defined list has special treatment
    [GenericTypeArguments(typeof(ReferenceType))] // non-string reference types use the default strategy
    public class Perf_Frozen<T>
    {
        protected T[] _array;
        protected FrozenSet<T> _frozenSet;
        protected Dictionary<T, T> _dictionary;
        protected FrozenDictionary<T, T> _frozenDictionary;

        [Params(4, // MaxItemsInSmallFrozenCollection
            64, // medium
            512)] // large
        public int Count;

        [GlobalSetup]
        public void Setup()
        {
            _array = GetUniqueValues(Count);
            _dictionary = _array.ToDictionary(item => item, item => item);
            _frozenDictionary = _dictionary.ToFrozenDictionary();
            _frozenSet = _array.ToFrozenSet();
        }

        protected T[] GetUniqueValues(int count)
        {
            if (typeof(T) == typeof(short))
            {
                return ValuesGenerator.ArrayOfUniqueValues<T>(count);
            }
            else if (typeof(T) == typeof(NotKnownComparable))
            {
                short[] shorts = ValuesGenerator.ArrayOfUniqueValues<short>(count);
                return (T[])(object)shorts.Select(value => new NotKnownComparable(value)).ToArray();
            }
            else
            {
                short[] shorts = ValuesGenerator.ArrayOfUniqueValues<short>(count);
                return (T[])(object)shorts.Select(value => new ReferenceType(value)).ToArray();
            }
        }

        [Benchmark]
        public FrozenDictionary<T, T> ToFrozenDictionary() => _dictionary.ToFrozenDictionary();

        [Benchmark]
        public bool TryGetValue_True()
        {
            bool result = default;
            var collection = _frozenDictionary;
            var found = _array;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public FrozenSet<T> ToFrozenSet() => _array.ToFrozenSet();

        [Benchmark]
        public bool Contains_True()
        {
            bool result = default;
            FrozenSet<T> collection = _frozenSet;
            T[] found = _array;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }
    }
}
