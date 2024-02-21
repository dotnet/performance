// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.RegularExpressions.Tests
{
    /// <summary>
    /// Performance tests for Regular Expressions
    /// </summary>
    [BenchmarkCategory(Categories.Libraries, Categories.Regex)]
    public class Perf_Regex_Common
    {
        private Regex _email, _date, _ip, _uri;
        private Regex _searchWord, _searchWords, _searchSet, _searchBoundary, _notOneLoopNodeBacktracking, _oneNodeBacktracking;
        private string _loremIpsum;

        [Params(RegexOptions.None, RegexOptions.Compiled, RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        public RegexOptions Options { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _email = new Regex(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,12}|[0-9]{1,3})(\]?)$", Options);
            _date = new Regex(@"\b\d{1,2}\/\d{1,2}\/\d{2,4}\b", Options);
            _ip = new Regex(@"(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9])\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9])", Options);
            _uri = new Regex(@"[\w]+://[^/\s?#]+[^\s?#]+(?:\?[^\s#]*)?(?:#[^\s]*)?", Options);

            _searchWord = new Regex(@"tempus", Options);
            _searchWords = new Regex(@"tempus|magna|semper", Options);
            _searchSet = new Regex(@"\w{10,}", Options);
            _searchBoundary = new Regex(@"\b\w{10,}\b", Options);
            _notOneLoopNodeBacktracking = new Regex(".*(ss)", Options);
            _oneNodeBacktracking = new Regex(@"[^a]+\.[^z]+", Options);
            _loremIpsum = LoremIpsum.ToString();
        }

        [Benchmark] public void Backtracking() => _notOneLoopNodeBacktracking.Match("Essential services are provided by regular exprs.");
        [Benchmark] [MemoryRandomization]
public void OneNodeBacktracking() => _oneNodeBacktracking.Match("This regex has the potential to be optimized further");
        [Benchmark] [MemoryRandomization]
public void Email_IsMatch() => _email.IsMatch("yay.performance@dot.net");
        [Benchmark] [MemoryRandomization]
public void Email_IsNotMatch() => _email.IsMatch("yay.performance@dot.net#");

        [Benchmark]         [MemoryRandomization]
public void Date_IsMatch() => _date.IsMatch("Today is 11/18/2019");
        [Benchmark] public void Date_IsNotMatch() => _date.IsMatch("Today is 11/18/201A");

        [Benchmark]         [MemoryRandomization]
public void IP_IsMatch() => _ip.IsMatch("012.345.678.910");
        [Benchmark] public void IP_IsNotMatch() => _ip.IsMatch("012.345.678.91A");

        [Benchmark] public void Uri_IsMatch() => _uri.IsMatch("http://example.org");
        [Benchmark] public void Uri_IsNotMatch() => _uri.IsMatch("http://a http://b");

        [Benchmark] public int MatchesSet() => _searchSet.Matches(_loremIpsum).Count;
        [Benchmark] public int MatchesBoundary() => _searchBoundary.Matches(_loremIpsum).Count;
        [Benchmark] public int MatchesWord() => _searchWord.Matches(_loremIpsum).Count;
        [Benchmark] public int MatchesWords() => _searchWords.Matches(_loremIpsum).Count;

        [Benchmark]         [MemoryRandomization]
public Match MatchWord() => _searchWords.Match(_loremIpsum);
        [Benchmark] public string ReplaceWords() => _searchWords.Replace(_loremIpsum, "amoveatur");
        [Benchmark] public string[] SplitWords() => _searchWords.Split(_loremIpsum);

        [Benchmark]         [MemoryRandomization]
public void Ctor() => new Regex(WarningPattern, Options);
        [Benchmark] [MemoryRandomization]
public void CtorInvoke() => new Regex(WarningPattern, Options).IsMatch(@"(1");

        private const string WarningPattern = @"(^(.*)(\(([0-9]+),([0-9]+)\)): )(error|warning) ([A-Z]+[0-9]+) ?: (.*)";

        private const string LoremIpsum =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer posuere eget magna a consectetur. Integer a libero volutpat, tempus lectus ac, aliquam enim. Etiam a ipsum nec mi vestibulum scelerisque. " +
            "Ut orci felis, efficitur molestie posuere id, eleifend vel urna. Duis lectus velit, iaculis nec ex non, consectetur blandit ex. Vestibulum nec mi suscipit, tempor purus at, convallis ligula. Sed auctor lobortis " +
            "porta. Donec nec quam non elit aliquet tristique sit amet id tortor. Nullam vitae cursus metus, vitae pulvinar nisl. Mauris dui nisi, lobortis eget placerat vel, tempus eu mauris. Suspendisse pretium egestas urna " +
            "vitae molestie.Mauris id odio mollis, sollicitudin odio in, tincidunt libero. Duis dolor nunc, placerat eu tincidunt eu, vehicula a arcu.Curabitur vitae eros libero. Nullam in eros enim. Praesent odio lorem, fringilla " +
            "ut eros id, tristique semper ipsum. Nunc accumsan magna nulla, sit amet pellentesque neque eleifend tempus. Pellentesque a fermentum nisi. Curabitur non facilisis diam. Pellentesque habitant morbi tristique senectus et " +
            "netus et malesuada fames ac turpis egestas. Curabitur finibus enim leo, vel luctus lacus volutpat sit amet. Suspendisse potenti. Curabitur nec ex lobortis, tincidunt nisl in, semper lacus. Maecenas bibendum suscipit elit, " +
            "et convallis augue pharetra eu.Donec vitae vestibulum diam. Suspendisse cursus vel augue quis elementum. Ut tempus quam purus. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.Proin lacus " +
            "diam, maximus a vulputate at, vehicula sit amet ipsum. Morbi lobortis libero dui, eu volutpat felis pellentesque in. Praesent sit amet venenatis nisl, quis semper lorem.Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
            "Etiam tempor lectus vitae est fermentum, et molestie leo egestas. Sed vitae magna nulla. Proin a aliquet mauris, id fringilla lorem.Fusce pellentesque, sapien et imperdiet scelerisque, sapien orci facilisis elit, sit " +
            "amet tristique nisi neque vitae mi. Maecenas sit amet imperdiet elit, euismod maximus est. Donec maximus, ex non efficitur bibendum, elit mi porta orci, vel commodo ipsum orci eu nisl. Integer maximus urna ac finibus blandit. " +
            "In hac habitasse platea dictumst.Suspendisse augue libero, lacinia eu lobortis venenatis, pretium eget mi.Cras eget feugiat est, in venenatis lectus. Sed luctus, sapien cursus semper ullamcorper, urna sapien consequat orci, " +
            "vitae congue mauris enim tempus neque. Sed in sagittis lacus. Nullam sodales interdum enim, venenatis sodales velit vehicula quis.Aliquam congue eu ex facilisis vestibulum. Nunc congue justo nulla, sit amet imperdiet mi " +
            "sodales in. Integer quis magna a sem euismod mollis ut ac eros. Vestibulum tincidunt scelerisque lacus. Ut ornare diam et purus gravida, quis aliquam dolor ultrices. Nulla elit arcu, eleifend id erat quis, tempus ultricies " +
            "lorem.Etiam suscipit magna vel nunc malesuada, nec faucibus nisi hendrerit. Nam sollicitudin, nisi at sodales egestas, urna odio blandit leo, non tristique lectus nunc ut libero. Aliquam pulvinar nulla vitae nibh venenatis, " +
            "sed euismod ligula egestas. Pellentesque malesuada congue sapien sit amet venenatis.Nulla blandit mi sit amet laoreet consectetur.Aliquam sodales non turpis ut suscipit. Morbi nec pretium risus, imperdiet accumsan nunc.Sed at " +
            "arcu augue. Curabitur dapibus aliquet felis et blandit. Maecenas.";
    }
}
