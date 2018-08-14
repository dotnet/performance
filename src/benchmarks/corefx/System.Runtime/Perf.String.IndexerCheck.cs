using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        private static readonly string _s1 =
            "ddsz dszdsz \t  dszdsz  a\u0300\u00C0 \t Te st \u0400Te \u0400st\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\t Te\t \tst \t\r\n\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005";

        [Benchmark]
        public int IndexerCheckBoundCheckHoist()
        {
            string s1 = _s1;
            int counter = 0;

            int strLength = _s1.Length;

            for (int j = 0; j < strLength; j++)
            {
                counter += s1[j];
            }

            return counter;
        }

        [Benchmark]
        public int IndexerCheckLengthHoisting()
        {
            string s1 = _s1;
            int counter = 0;

            for (int j = 0; j < s1.Length; j++)
            {
                counter += s1[j];
            }

            return counter;
        }

        [Benchmark]
        public int IndexerCheckPathLength()
        {
            string s1 = _s1;
            int counter = 0;

            for (int j = 0; j < s1.Length; j++)
            {
                counter += getStringCharNoInline(s1, j);
            }

            return counter;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static char getStringCharNoInline(string str, int index)
        {
            return str[index];
        }
    }
}