using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        public static IEnumerable<object[]> FormatArgs1 => Permutations(
            new object[] {"Testing {0}, {0:C}, {0:D5}, {0:E} - {0:F4}{0:G}{0:N}  {0:X} !!"},
            new object[] {8, 0}
        );

        public static IEnumerable<object[]> FormatArgs2 => Permutations(
            new object[] {"Testing {0}, {0:C}, {0:E} - {0:F4}{0:G}{0:N} , !!"},
            new object[] {0, -2, 3.14159, 11000000}
        );

        public static IEnumerable<object[]> FormatArgs3 => Permutations(
            new object[] {"More testing: {0}"},
            new object[]
                {0, -2, 3.14159, 11000000, "Foo", 'a', "Testing {0}, {0:C}, {0:D5}, {0:E} - {0:F4}{0:G}{0:N}  {0:X} !!"}
        );

        public static IEnumerable<object[]> AllFormatArgs => FormatArgs1.Concat(FormatArgs2).Concat(FormatArgs3);

        [Benchmark]
        [ArgumentsSource(nameof(AllFormatArgs))]
        public string Format_OneArg(string s, object o)
            => string.Format(s, o);

        [Benchmark]
        public string Format_MultipleArgs()
            => string.Format("More testing: {0} {1} {2} {3} {4} {5}{6} {7}", '1', "Foo", "Foo", "Foo", "Foo", "Foo", "Foo", "Foo");
    }
}