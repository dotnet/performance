using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        [Benchmark]
        [Arguments("This is a very nice sentence", 'z', 'y')] // 'z' does not exist in the string
        [Arguments("This is a very nice sentence", 'i', 'I')] // 'i' occuress 3 times in the string
        public string Replace_Char(string text, char oldChar, char newChar)
            => text.Replace(oldChar, newChar);

        [Benchmark]
        [Arguments("This is a very nice sentence", "bad", "nice")] // there are no "bad" words in the string
        [Arguments("This is a very nice sentence", "nice", "bad")] // there are is one "nice" word in the string
        public string Replace_String(string text, string oldValue, string newValue)
            => text.Replace(oldValue, newValue);
    }
}