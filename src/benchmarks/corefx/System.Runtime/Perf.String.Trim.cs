using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        [Benchmark]
        [Arguments("Test")]
        [Arguments(" Test")]
        [Arguments("Test ")]
        [Arguments(" Te st  ")]
        public string Trim(string s)
            => s.Trim();

        [Benchmark]
        [Arguments("Test")]
        [Arguments(" Test")]
        public string TrimStart(string s)
            => s.TrimStart();

        [Benchmark]
        [Arguments("Test")]
        [Arguments("Test ")]
        public string TrimEnd(string s)
            => s.TrimEnd();
        
        [Benchmark]
        [Arguments("Test", new [] {' ', (char) 8197})]
        [Arguments(" Test", new [] {' ', (char) 8197})]
        [Arguments("Test ", new [] {' ', (char) 8197})]
        [Arguments(" Te st  ", new [] {' ', (char) 8197})]
        public string Trim_CharArr(string s, char[] c)
            => s.Trim(c);

        [Benchmark]
        [Arguments("Test", new [] {' ', (char) 8197})]
        [Arguments(" Test", new [] {' ', (char) 8197})]
        public string TrimStart_CharArr(string s, char[] c)
            => s.TrimStart(c);

        [Benchmark]
        [Arguments("Test", new [] {' ', (char) 8197})]
        [Arguments("Test ", new [] {' ', (char) 8197})]
        public string TrimEnd_CharArr(string s, char[] c)
            => s.TrimEnd(c);
    }
}