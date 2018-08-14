using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        [Benchmark]
        [Arguments("Testing {0}, {0:C}, {0:D5}, {0:E} - {0:F4}{0:G}{0:N}  {0:X} !!", 8)]
        [Arguments("Testing {0}, {0:C}, {0:E} - {0:F4}{0:G}{0:N} , !!", 3.14159)]
        public string Format_OneArg(string s, object o)
            => string.Format(s, o);

        [Benchmark]
        public string Format_MultipleArgs()
            => string.Format("More testing: {0} {1} {2} {3} {4} {5}{6} {7}", '1', "Foo", "Foo", "Foo", "Foo", "Foo", "Foo", "Foo");
    }
}