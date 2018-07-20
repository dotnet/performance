using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.NonGenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type (it shows how bad idea is to use non-generic collections for value types)
    [GenericTypeArguments(typeof(string))] // reference type
    public class IterateForNonGeneric<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;
        
        private ArrayList _arraylist;

        [GlobalSetup(Target = nameof(ArrayList))]
        public void SetupArrayList() => _arraylist = new ArrayList(UniqueValuesGenerator.GenerateArray<T>(Size));
        
        [Benchmark]
        public object ArrayList()
        {
            object result = default;
            var collection = _arraylist;
            for(int i = 0; i < collection.Count; i++)
                result = collection[i];
            return result;
        }
    }
}