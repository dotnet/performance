using BenchmarkDotNet.Attributes;
using Benchmarks;
using Helpers;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.NonGenericCollections)]
    public class IterateForNonGeneric
    {
        [Params(100)]
        public int Size;
        
        private ArrayList _arraylist;

        [GlobalSetup(Target = nameof(ArrayList))]
        public void SetupArrayList() => _arraylist = new ArrayList(UniqueValuesGenerator.GenerateArray<string>(Size));
        
        [Benchmark]
        public object ArrayList()
        {
            object result = default;
            var local = _arraylist;
            for(int i = 0; i < local.Count; i++)
                result = local[i];
            return result;
        }
    }
}