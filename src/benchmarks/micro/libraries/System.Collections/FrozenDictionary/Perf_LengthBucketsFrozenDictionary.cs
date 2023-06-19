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
    public class Perf_LengthBucketsFrozenDictionary
    {
        private string[] _perLengthArray;
        private Dictionary<string, string> _perLengthDictionary;
        private FrozenDictionary<string, string> _frozenDictionary, _frozenDictionaryOptimized;

        [Params(10, 100, 1000, 10_000)]
        public int Count;

        [Params(1, 5)]
        public int ItemsPerBucket;

        [GlobalSetup]
        public void LengthBucketsSetup()
        {
            if (Count % ItemsPerBucket != 0)
            {
                throw new ArgumentException($"{nameof(Count)} needs to be a multiply of {nameof(ItemsPerBucket)}");
            }

            _perLengthArray = Enumerable.Range(1, Count / ItemsPerBucket)
                .SelectMany(length => Enumerable.Range('a', ItemsPerBucket).Select(character => new string((char)character, length)))
                .ToArray();
            _perLengthDictionary = _perLengthArray.ToDictionary(item => item, item => item);
            _frozenDictionary = _perLengthDictionary.ToFrozenDictionary(optimizeForReading: false);
            _frozenDictionaryOptimized = _perLengthDictionary.ToFrozenDictionary(optimizeForReading: true);

            if (!_frozenDictionaryOptimized.GetType().Name.Contains("LengthBucketsFrozenDictionary"))
            {
                throw new InvalidOperationException("Either we are using wrong strategy, or the type has been renamed.");
            }
        }

        [BenchmarkCategory("Creation")]
        [Benchmark(Baseline = true)]
        public Dictionary<string, string> ToDictionary() => new(_perLengthDictionary);

        [BenchmarkCategory("Creation")]
        [Benchmark]
        public FrozenDictionary<string, string> ToFrozenDictionary() => _perLengthDictionary.ToFrozenDictionary(optimizeForReading: false);

        [BenchmarkCategory("Creation")]
        [Benchmark]
        public FrozenDictionary<string, string> ToFrozenDictionary_Optimized() => _perLengthDictionary.ToFrozenDictionary(optimizeForReading: true);

        [BenchmarkCategory("TryGetValue")]
        [Benchmark(Baseline = true)]
        public bool TryGetValue_True_Dictionary()
        {
            bool result = default;
            var collection = _perLengthDictionary;
            string[] found = _perLengthArray;
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
            string[] found = _perLengthArray;
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
            string[] found = _perLengthArray;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.TryGetValue(found[i], out _);
            return result;
        }
    }
}
