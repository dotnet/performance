// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace System.Text.RegularExpressions.Tests
{
    internal static class Perf_Regex_Industry
    {
        public static string ReadInputFile(string name)
        {
            string path = Path.Combine(Path.GetDirectoryName(typeof(Perf_Regex_Industry).Assembly.Location), "libraries", "System.Text.RegularExpressions", "TestData", name);
            using (FileStream fs = File.OpenRead(path))
            using (var gz = new GZipStream(fs, CompressionMode.Decompress))
            using (var reader = new StreamReader(gz))
            {
                return reader.ReadToEnd();
            }
        }
    }

    /// <summary>Performance tests adapted from https://github.com/mariomka/regex-benchmark</summary>
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Regex_Industry_Mariomka
    {
        [Params(
            @"[\w\.+-]+@[\w\.-]+\.[\w\.-]+",
            @"[\w]+://[^/\s?#]+[^\s?#]+(?:\?[^\s#]*)?(?:#[^\s]*)?",
            @"(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9])\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9])"
        )]
        public string Pattern { get; set; }

        [Params(RegexOptions.None, RegexOptions.Compiled
#if NET7_0_OR_GREATER
            , RegexOptions.NonBacktracking
#endif
            )]
        public RegexOptions Options { get; set; }

        private string _input_text;
        private Regex _regex;

        [Benchmark]
        public Regex Ctor() => new Regex(Pattern, Options);

        [GlobalSetup(Target = nameof(CountMatches))]
        public void Setup()
        {
            _regex = new Regex(Pattern, Options);
            _input_text = Perf_Regex_Industry.ReadInputFile("mariomka.txt.gz");
        }

        [Benchmark]
        public int CountMatches() => _regex.Matches(_input_text).Count;
    }

    /// <summary>Performance tests adapted from https://github.com/cloudflare/sliceslice-rs/tree/a27b76c8527d44d5b3534c84b878d8289eacb7ff/data</summary>
    [BenchmarkCategory(Categories.Libraries)]
    [SkipTooManyTestCasesValidator]
    public class Perf_Regex_Industry_SliceSlice
    {
        [Params(RegexOptions.None, RegexOptions.Compiled
#if NET7_0_OR_GREATER
            , RegexOptions.NonBacktracking
#endif
            )]
        public RegexOptions Options { get; set; }

        private string _i386;
        private Regex[] _regexes;

        [GlobalSetup(Target = nameof(CountMatches))]
        public void Setup()
        {
            _i386 = Perf_Regex_Industry.ReadInputFile("i386.txt.gz");
            string words = Perf_Regex_Industry.ReadInputFile("words.txt.gz");
            _regexes = words.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(word => new Regex(word, Options)).ToArray();
        }

        [Benchmark]
        public int CountMatches()
        {
            int found = 0;
            foreach (Regex r in _regexes)
            {
                Match m = r.Match(_i386);
                while (m.Success)
                {
                    found++;
                    m = m.NextMatch();
                }
            }
            return found;
        }
    }

    /// <summary>Performance tests adapted from https://github.com/rust-lang/regex</summary>
    [BenchmarkCategory(Categories.Libraries)]
    [SkipTooManyTestCasesValidator]
    public class Perf_Regex_Industry_RustLang_Sherlock
    {
        [Params(
            @"Sherlock",
            @"Holmes",
            @"Sherlock Holmes",
            @"(?i)Sherlock",
            @"(?i)Holmes",
            @"(?i)Sherlock Holmes",
            @"Sherlock\s+Holmes",
            @"Sherlock|Street",
            @"Sherlock|Holmes",
            @"Sherlock|Holmes|Watson|Irene|Adler|John|Baker",
            @"(?i)Sherlock|Holmes|Watson|Irene|Adler|John|Baker",
            @"Sher[a-z]+|Hol[a-z]+",
            @"(?i)Sher[a-z]+|Hol[a-z]+",
            @"Sherlock|Holmes|Watson",
            @"(?i)Sherlock|Holmes|Watson",
            @"zqj",
            @"aqj",
            @"aei",
            @"the",
            @"The",
            @"(?i)the",
            @"the\s+\w+",
            @".*",
            @"[^\n]*",
            @"(?s).*",
            @"\p{L}",
            @"\p{Lu}",
            @"\p{Ll}",
            @"\w+",
            @"\w+\s+Holmes",
            @"\w+\s+Holmes\s+\w+",
            @"Holmes.{0,25}Watson|Watson.{0,25}Holmes",
            //@"Holmes(?:\s*.+\s*){0,10}Watson|Watson(?:\s*.+\s*){0,10}Holmes", // Too slow with backtracking engine
            //@"[""'][^""']{0,30}[?!.][""']", // Breaks benchmarkdotnet 13.1
            @"(?m)^Sherlock Holmes|Sherlock Holmes$",
            @"\b\w+n\b",
            @"[a-q][^u-z]{13}x",
            @"[a-zA-Z]+ing",
            @"\s[a-zA-Z]{0,12}ing\s"
        )]
        public string Pattern { get; set; }

        [Params(RegexOptions.None, RegexOptions.Compiled
#if NET7_0_OR_GREATER
            , RegexOptions.NonBacktracking
#endif
            )]
        public RegexOptions Options { get; set; }


        private string _sherlock;
        private Regex _regex;

        [GlobalSetup(Target = nameof(CountMatches))]
        public void Setup()
        {
            _regex = new Regex(Pattern, Options);
            _sherlock = Perf_Regex_Industry.ReadInputFile("sherlock.txt.gz");
        }

        [Benchmark]
        public int CountMatches()
        {
            int found = 0;
            Match m = _regex.Match(_sherlock);
            while (m.Success)
            {
                found++;
                m = m.NextMatch();
            }
            return found;
        }
    }

    /// <summary>Performance tests adapted from https://rust-leipzig.github.io/regex/2017/03/28/comparison-of-regex-engines/</summary>
    [BenchmarkCategory(Categories.Libraries)]
    [SkipTooManyTestCasesValidator]
    public class Perf_Regex_Industry_Leipzig
    {
        [Params(
            "Twain",
            "(?i)Twain",
            "[a-z]shing",
            "Huck[a-zA-Z]+|Saw[a-zA-Z]+",
            "\\b\\w+nn\\b",
            //"[a-q][^u-z]{13}x", // duplicates Perf_Regex_Industry_RustLang_Sherlock test
            "Tom|Sawyer|Huckleberry|Finn",
            "(?i)Tom|Sawyer|Huckleberry|Finn",
            ".{0,2}(Tom|Sawyer|Huckleberry|Finn)",
            ".{2,4}(Tom|Sawyer|Huckleberry|Finn)",
            "Tom.{10,25}river|river.{10,25}Tom",
            //"[a-zA-Z]+ing", // duplicates Perf_Regex_Industry_RustLang_Sherlock test
            "\\s[a-zA-Z]{0,12}ing\\s",
            "([A-Za-z]awyer|[A-Za-z]inn)\\s",
            "[\"'][^\"']{0,30}[?!\\.][\"']",
            "\u221E|\u2713",
            "\\p{Sm}")]
        public string Pattern { get; set; }

        [Params(RegexOptions.None, RegexOptions.Compiled
#if NET7_0_OR_GREATER
            , RegexOptions.NonBacktracking
#endif
            )]
        public RegexOptions Options { get; set; }

        private string _3200;
        private Regex _regex;

        [GlobalSetup(Target = nameof(CountMatches))]
        public void Setup()
        {
            _regex = new Regex(Pattern, Options);
            _3200 = Perf_Regex_Industry.ReadInputFile("3200.txt.gz");
        }

        [Benchmark]
        public int CountMatches()
        {
            int found = 0;
            Match m = _regex.Match(_3200);
            while (m.Success)
            {
                found++;
                m = m.NextMatch();
            }
            return found;
        }
    }

    /// <summary>Performance tests adapted from https://www.boost.org/doc/libs/1_41_0/libs/regex/doc/gcc-performance.html</summary>
    [BenchmarkCategory(Categories.Libraries)]
    [SkipTooManyTestCasesValidator]
    public class Perf_Regex_Industry_BoostDocs_Simple
    {
        [Params(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13)]
        public int Id { get; set; }

        [Params(RegexOptions.None, RegexOptions.Compiled
#if NET7_0_OR_GREATER
            , RegexOptions.NonBacktracking
#endif
            )]
        public RegexOptions Options { get; set; }

        private string _input;
        private Regex _regex;

        [GlobalSetup(Target = nameof(IsMatch))]
        public void Setup()
        {
            _input = Id switch
            {
                0 => @"abc",
                1 => @"100- this is a line of ftp response which contains a message string",
                2 => @"1234-5678-1234-456",
                3 => @"john@johnmaddock.co.uk",
                4 => @"foo12@foo.edu",
                5 => @"bob.smith@foo.tv",
                6 => @"EH10 2QQ",
                7 => @"G1 1AA",
                8 => @"SW1 1ZZ",
                9 => @"4/1/2001",
                10 => @"12/12/2001",
                11 => @"123",
                12 => @"+3.14159",
                _ => @"-3.14159",
            };

            _regex = new Regex(Id switch
            {
                0  => @"abc",
                1  => @"^([0-9]+)(\-| |$)(.*)$",
                2  => @"(\d{4}[- ]){3}\d{3,4}",
                3  => @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                4  => @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                5  => @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                6  => @"^[a-zA-Z]{1,2}[0-9][0-9A-Za-z]{0,1} {0,1}[0-9][A-Za-z]{2}$",
                7  => @"^[a-zA-Z]{1,2}[0-9][0-9A-Za-z]{0,1} {0,1}[0-9][A-Za-z]{2}$",
                8  => @"^[a-zA-Z]{1,2}[0-9][0-9A-Za-z]{0,1} {0,1}[0-9][A-Za-z]{2}$",
                9  => @"^\d{1,2}/\d{1,2}/\d{4}$",
                10 => @"^\d{1,2}/\d{1,2}/\d{4}$",
                11 => @"^[-+]?\d*\.?\d*$",
                12 => @"^[-+]?\d*\.?\d*$",
                _  => @"^[-+]?\d*\.?\d*$",
            }, Options);
        }

        [Benchmark]
        public bool IsMatch() => _regex.IsMatch(_input);
    }
}
