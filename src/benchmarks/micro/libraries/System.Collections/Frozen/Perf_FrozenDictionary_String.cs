using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace System.Collections
{
    // We don't want to run all these benchmarks for every CI run, that is why they belong to ThirdParty category.
    // The benchmarks that we care about the most: optimized frozen dictionary benchmarks belong to Libraries category.
    [BenchmarkCategory(Categories.ThirdParty)]
    public abstract class Perf_FrozenDictionary_String
    {
        protected string[] _array;
        protected Dictionary<string, string> _dictionary;
        protected ImmutableDictionary<string, string> _immutableDictionary;
        protected FrozenDictionary<string, string> _frozenDictionary;

        [GlobalSetup]
        public abstract void Setup();

        [Benchmark]
        public Dictionary<string, string> ToDictionary() => new(_dictionary);

        [Benchmark]
        public ImmutableDictionary<string, string> ToImmutableDictionary() => _dictionary.ToImmutableDictionary();

        [Benchmark]
        [BenchmarkCategory(Categories.Libraries)]
        public FrozenDictionary<string, string> ToFrozenDictionary() => _dictionary.ToFrozenDictionary();

        [Benchmark]
        public bool TryGetValue_True_Dictionary()
        {
            bool result = default;
            var collection = _dictionary;
            string[] found = _array;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.TryGetValue(found[i], out _);
            return result;
        }

        [Benchmark]
        public bool TryGetValue_True_ImmutableDictionary()
        {
            bool result = default;
            var collection = _immutableDictionary;
            string[] found = _array;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.TryGetValue(found[i], out _);
            return result;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.Libraries)]
        public bool TryGetValue_True_FrozenDictionary()
        {
            bool result = default;
            var collection = _frozenDictionary;
            string[] found = _array;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.TryGetValue(found[i], out _);
            return result;
        }

        protected void EnsureRightStrategyIsUsed(string name)
        {
            if (!_frozenDictionary.GetType().Name.Contains(name))
            {
                throw new InvalidOperationException($"Either we are using wrong strategy ({_frozenDictionary.GetType().Name}), or the type has been renamed.");
            }
        }
    }

    public class Perf_LengthBucketsFrozenDictionary : Perf_FrozenDictionary_String
    {
        [Params(10, 100, 1000, 10_000)]
        public int Count;

        [Params(1, 5)]
        public int ItemsPerBucket;

        public override void Setup()
        {
            if (Count % ItemsPerBucket != 0)
            {
                throw new ArgumentException($"{nameof(Count)} needs to be a multiply of {nameof(ItemsPerBucket)}");
            }

            _array = Enumerable.Range(1, Count / ItemsPerBucket)
                .SelectMany(length => Enumerable.Range('a', ItemsPerBucket).Select(character => new string((char)character, length)))
                .ToArray();
            _dictionary = _array.ToDictionary(item => item, item => item);
            _immutableDictionary = _dictionary.ToImmutableDictionary();
            _frozenDictionary = _dictionary.ToFrozenDictionary();

            EnsureRightStrategyIsUsed("LengthBucketsFrozenDictionary");
        }
    }

    public class Perf_SingleCharFrozenDictionary : Perf_FrozenDictionary_String
    {
        [Params(10, 100, 1000, 10_000)]
        public int Count;

        public override void Setup()
        {
            _array = Enumerable.Range(char.MinValue, Count)
                .Select(character => new string((char)character, 10))
                .ToArray();
            _dictionary = _array.ToDictionary(item => item, item => item);
            _immutableDictionary = _dictionary.ToImmutableDictionary();
            _frozenDictionary = _dictionary.ToFrozenDictionary();

            EnsureRightStrategyIsUsed("SingleChar");
        }
    }

    public class Perf_SubstringFrozenDictionary : Perf_FrozenDictionary_String
    {
        [Params(10, 100, 1000, 10_000)]
        public int Count;

        public override void Setup()
        {
            if (Count % 2 != 0)
            {
                throw new ArgumentException($"{nameof(Count)} needs to be a multiply of 2");
            }

            // Generate sth like:
            // abaaaaaa
            // acaaaaaa
            // bcbbbbbb
            // bdbbbbbb
            // so the first char is not unique, but the combination of 1st and 2nd is.
            _array = Enumerable.Range(char.MinValue, Count / 2)
                .SelectMany(character => new string[]
                {
                    $"{(char)character}{(char)(character+1)}{new string((char)character, 8)}",
                    $"{(char)character}{(char)(character+2)}{new string((char)character, 8)}"
                })
                .ToArray();
            _dictionary = _array.ToDictionary(item => item, item => item);
            _immutableDictionary = _dictionary.ToImmutableDictionary();
            _frozenDictionary = _dictionary.ToFrozenDictionary();

            EnsureRightStrategyIsUsed("Substring");
        }
    }

    public class Perf_DefaultFrozenDictionary : Perf_FrozenDictionary_String
    {
        [Params(10, 100, 1000, 10_000)]
        public int Count;

        public override void Setup()
        {
            _array = Count == 10
                // to avoid using LengthBucketsFrozenDictionary we specify the same length for more than 5 strings
                ? ValuesGenerator.ArrayOfUniqueStrings(10, minLength: 25, maxLength: 25)
                : ValuesGenerator.ArrayOfUniqueValues<string>(Count);

            _dictionary = _array.ToDictionary(item => item, item => item);
            _immutableDictionary = _dictionary.ToImmutableDictionary();
            _frozenDictionary = _dictionary.ToFrozenDictionary();

            EnsureRightStrategyIsUsed("OrdinalStringFrozenDictionary");
        }
    }
}
