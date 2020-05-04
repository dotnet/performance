using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    class ExclusionFilter : IFilter
    {
        private readonly GlobFilter globFilter;

        public ExclusionFilter(string _filter)
        {
            if (!String.IsNullOrEmpty(_filter))
            {
                globFilter = new GlobFilter(new string[] { _filter });
            }
        }

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            if(globFilter == null)
            {
                return true;
            }
            return !globFilter.Predicate(benchmarkCase);
        }
    }
}
