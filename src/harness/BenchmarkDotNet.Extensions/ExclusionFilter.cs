using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    class ExclusionFilter : IFilter
    {
        private string filter;

        public ExclusionFilter(string _filter)
        {
            if (!String.IsNullOrEmpty(_filter))
            {
                filter = _filter.ToLowerInvariant();
            }
        }

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            if(String.IsNullOrEmpty(filter))
            {
                return true;
            }
            string testName = benchmarkCase.DisplayInfo.ToLowerInvariant().Substring(0, benchmarkCase.DisplayInfo.IndexOf(':') + 1);
            return !testName.Contains(filter);
        }
    }
}
