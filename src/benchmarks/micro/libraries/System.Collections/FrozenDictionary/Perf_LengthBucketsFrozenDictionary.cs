using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MicroBenchmarks;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace System.Collections
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByJob, BenchmarkLogicalGroupRule.ByCategory)]
    [BenchmarkCategory(Categories.Libraries)]
    public abstract class Perf_FrozenDictionary
    {
        protected string[] _array;
        protected Dictionary<string, string> _dictionary;
        protected FrozenDictionary<string, string> _frozenDictionary, _frozenDictionaryOptimized;

        [GlobalSetup]
        public abstract void Setup();

        [BenchmarkCategory("Creation")]
        [Benchmark(Baseline = true)]
        public Dictionary<string, string> ToDictionary() => new(_dictionary);

        [BenchmarkCategory("Creation")]
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
            _frozenDictionary = _dictionary.ToFrozenDictionary(optimizeForReading: false);
            _frozenDictionaryOptimized = _dictionary.ToFrozenDictionary(optimizeForReading: true);

            if (!_frozenDictionaryOptimized.GetType().Name.Contains("LengthBucketsFrozenDictionary"))
            {
                throw new InvalidOperationException("Either we are using wrong strategy, or the type has been renamed.");
            }
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
            _frozenDictionary = _dictionary.ToFrozenDictionary(optimizeForReading: false);
            _frozenDictionaryOptimized = _dictionary.ToFrozenDictionary(optimizeForReading: true);

            if (!_frozenDictionaryOptimized.GetType().Name.Contains("SingleChar"))
            {
                throw new InvalidOperationException("Either we are using wrong strategy, or the type has been renamed.");
            }
        }
    }
}
