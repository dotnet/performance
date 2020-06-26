using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.Extensions
{
    class ExclusionFilter : IFilter
    {
        private readonly GlobFilter globFilter;

        public ExclusionFilter(List<string> _filter)
        {
            if (_filter != null && _filter.Count != 0)
            {
                globFilter = new GlobFilter(_filter.ToArray());
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

    class CategoryExclusionFilter : IFilter
    {
        private readonly List<(string userValue, Regex regex)> patterns;

        public CategoryExclusionFilter(List<string> patterns)
        {
            if (patterns != null)
            {
                this.patterns = patterns.Select(pattern => (pattern, new Regex(WildcardToRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))).ToList();
            }
            else
            {
                patterns = null;
            }
        }

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            if(patterns == null)
            {
                return true;
            }
            foreach (var category in benchmarkCase.Descriptor.Categories)
            {
                if(patterns.Any(pattern => category.Equals(pattern.userValue, StringComparison.OrdinalIgnoreCase) || pattern.regex.IsMatch(category)))
                {
                    return false;
                }
            }

            return true;
        }

        // https://stackoverflow.com/a/6907849/5852046 not perfect but should work for all we need
        private static string WildcardToRegex(string pattern) => $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";
    }
}
