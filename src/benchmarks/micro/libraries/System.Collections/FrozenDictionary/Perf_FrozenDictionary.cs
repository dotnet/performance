using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace System.Collections
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByJob, BenchmarkLogicalGroupRule.ByCategory)]
    [BenchmarkCategory(Categories.Libraries)]
    public abstract class Perf_FrozenDictionary
    {
        protected string[] _array;
        protected Dictionary<string, string> _dictionary;
        protected ImmutableDictionary<string, string> _immutableDictionary;
        protected FrozenDictionary<string, string> _frozenDictionary, _frozenDictionaryOptimized;

        [GlobalSetup]
        public abstract void Setup();

        [BenchmarkCategory("Creation")]
        [Benchmark(Baseline = true)]
        public Dictionary<string, string> ToDictionary() => new(_dictionary);

        [BenchmarkCategory("Creation")]
        [Benchmark]
        public ImmutableDictionary<string, string> ToImmutableDictionary() => _dictionary.ToImmutableDictionary();

        [Benchmark]
        public FrozenDictionary<string, string> ToFrozenDictionary() => _dictionary.ToFrozenDictionary(optimizeForReading: false);

        [BenchmarkCategory("Creation")]
        [Benchmark]
        public FrozenDictionary<string, string> ToFrozenDictionary_Optimized() => _dictionary.ToFrozenDictionary(optimizeForReading: true);

        [BenchmarkCategory("TryGetValue")]
        [Benchmark(Baseline = true)]
        public bool TryGetValue_True_Dictionary()
        {
            bool result = default;
            var collection = _dictionary;
            string[] found = _array;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.TryGetValue(found[i], out _);
            return result;
        }

        [BenchmarkCategory("TryGetValue")]
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
        public bool TryGetValue_True_FrozenDictionary()
        {
            bool result = default;
            var collection = _frozenDictionary;
            string[] found = _array;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.TryGetValue(found[i], out _);
            return result;
        }

        [BenchmarkCategory("TryGetValue")]
        [Benchmark]
        public bool TryGetValue_True_FrozenDictionaryOptimized()
        {
            bool result = default;
            var collection = _frozenDictionaryOptimized;
            string[] found = _array;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.TryGetValue(found[i], out _);
            return result;
        }

        protected void EnsureRightStrategyIsUsed(string name)
        {
            if (!_frozenDictionaryOptimized.GetType().Name.Contains(name))
            {
                throw new InvalidOperationException("Either we are using wrong strategy, or the type has been renamed.");
            }
        }
    }


    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByJob, BenchmarkLogicalGroupRule.ByCategory)]
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_LengthBucketsFrozenDictionary : Perf_FrozenDictionary
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
            _frozenDictionary = _dictionary.ToFrozenDictionary(optimizeForReading: false);
            _frozenDictionaryOptimized = _dictionary.ToFrozenDictionary(optimizeForReading: true);

            EnsureRightStrategyIsUsed("LengthBucketsFrozenDictionary");
        }
    }

    public class Perf_SingleCharFrozenDictionary : Perf_FrozenDictionary
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
            _frozenDictionary = _dictionary.ToFrozenDictionary(optimizeForReading: false);
            _frozenDictionaryOptimized = _dictionary.ToFrozenDictionary(optimizeForReading: true);

            EnsureRightStrategyIsUsed("SingleChar");
        }
    }

    public class Perf_SubstringFrozenDictionary : Perf_FrozenDictionary
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
            _frozenDictionary = _dictionary.ToFrozenDictionary(optimizeForReading: false);
            _frozenDictionaryOptimized = _dictionary.ToFrozenDictionary(optimizeForReading: true);

            EnsureRightStrategyIsUsed("Substring");
    }

    public class Perf_DefaultFrozenDictionary : Perf_FrozenDictionary
    {
        [Params(10, 100, 1000, 10_000)]
        public int Count;

        public override void Setup()
        {
            _array = ValuesGenerator.ArrayOfUniqueValues<string>(Count);
            _dictionary = _array.ToDictionary(item => item, item => item);
            _immutableDictionary = _dictionary.ToImmutableDictionary();
            _frozenDictionary = _dictionary.ToFrozenDictionary(optimizeForReading: false);
            _frozenDictionaryOptimized = _dictionary.ToFrozenDictionary(optimizeForReading: true);

            EnsureRightStrategyIsUsed("OrdinalStringFrozenDictionary");
        }
    }
}
