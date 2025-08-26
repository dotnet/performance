using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using System.Collections.Generic;

namespace BenchmarkDotNet.Extensions
{
    // Includes only benchmarks whose FullName (via FullNameProvider) exists in provided hash set
    public class TestNameFilter : IFilter
    {
        private readonly HashSet<string> _allowedNames; // empty or null => disabled

        public TestNameFilter(HashSet<string> allowedNames)
        {
            _allowedNames = allowedNames;
        }

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            if (_allowedNames == null || _allowedNames.Count == 0)
                return true; // disabled

            var fullName = FullNameProvider.GetBenchmarkName(benchmarkCase);
            return _allowedNames.Contains(fullName);
        }
    }
}
