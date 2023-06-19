// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Adapted from regex-redux C# .NET Core #5 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=regexredux&lang=csharpcore&id=5
// aka (as of 2017-09-01) rev 1.3 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/regexredux/regexredux.csharp-5.csharp?root=benchmarksgame&view=log
// Best-scoring C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 
   Regex-Redux by Josh Goldfoot
   order variants by execution time by Anthony Lloyd
*/

using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace BenchmarksGame
{
    [BenchmarkCategory(Categories.Runtime, Categories.BenchmarksGame, Categories.JIT, Categories.Regex, Categories.NoWASM)]
    public class RegexRedux_5
    {
        private string _sequences;

        [GlobalSetup]
        public void Setup()
        {
            RegexReduxHelpers helpers = new RegexReduxHelpers(bigInput: true);

            using (var inputStream = new FileStream(helpers.InputFile, FileMode.Open))
            using (var input = new StreamReader(inputStream))
            {
                _sequences = input.ReadToEnd();
            }
        }
        
        [Benchmark(Description = nameof(RegexRedux_5))]
        [Arguments(RegexOptions.Compiled)]
        [Arguments(RegexOptions.None)]
        public int RunBench(RegexOptions options)
        {
            var sequences = _sequences;
            sequences = Regex.Replace(sequences, ">.*\n|\n", "", options);

            var magicTask = Task.Run(() =>
            {
                var newseq = Regex.Replace(sequences, "tHa[Nt]", "<4>", options);
                newseq = Regex.Replace(newseq, "aND|caN|Ha[DS]|WaS", "<3>", options);
                newseq = Regex.Replace(newseq, "a[NSt]|BY", "<2>", options);
                newseq = Regex.Replace(newseq, "<[^>]*>", "|", options);
                newseq = Regex.Replace(newseq, "\\|[^|][^|]*\\|", "-", options);
                return newseq.Length;
            });

#if NET7_0_OR_GREATER
            var variant2 = Task.Run(() => "[cgt]gggtaaa|tttaccc[acg] " + Regex.Count(sequences, "[cgt]gggtaaa|tttaccc[acg]", options));
            var variant3 = Task.Run(() => "a[act]ggtaaa|tttacc[agt]t " + Regex.Count(sequences, "a[act]ggtaaa|tttacc[agt]t", options));
            var variant7 = Task.Run(() => "agggt[cgt]aa|tt[acg]accct " + Regex.Count(sequences, "agggt[cgt]aa|tt[acg]accct", options));
            var variant6 = Task.Run(() => "aggg[acg]aaa|ttt[cgt]ccct " + Regex.Count(sequences, "aggg[acg]aaa|ttt[cgt]ccct", options));
            var variant4 = Task.Run(() => "ag[act]gtaaa|tttac[agt]ct " + Regex.Count(sequences, "ag[act]gtaaa|tttac[agt]ct", options));
            var variant5 = Task.Run(() => "agg[act]taaa|ttta[agt]cct " + Regex.Count(sequences, "agg[act]taaa|ttta[agt]cct", options));
            var variant1 = Task.Run(() => "agggtaaa|tttaccct " +         Regex.Count(sequences, "agggtaaa|tttaccct", options));
            var variant9 = Task.Run(() => "agggtaa[cgt]|[acg]ttaccct " + Regex.Count(sequences, "agggtaa[cgt]|[acg]ttaccct", options));
            var variant8 = Task.Run(() => "agggta[cgt]a|t[acg]taccct " + Regex.Count(sequences, "agggta[cgt]a|t[acg]taccct", options));
#else
            var variant2 = Task.Run(() => regexCount(sequences, "[cgt]gggtaaa|tttaccc[acg]", options));
            var variant3 = Task.Run(() => regexCount(sequences, "a[act]ggtaaa|tttacc[agt]t", options));
            var variant7 = Task.Run(() => regexCount(sequences, "agggt[cgt]aa|tt[acg]accct", options));
            var variant6 = Task.Run(() => regexCount(sequences, "aggg[acg]aaa|ttt[cgt]ccct", options));
            var variant4 = Task.Run(() => regexCount(sequences, "ag[act]gtaaa|tttac[agt]ct", options));
            var variant5 = Task.Run(() => regexCount(sequences, "agg[act]taaa|ttta[agt]cct", options));
            var variant1 = Task.Run(() => regexCount(sequences, "agggtaaa|tttaccct", options));
            var variant9 = Task.Run(() => regexCount(sequences, "agggtaa[cgt]|[acg]ttaccct", options));
            var variant8 = Task.Run(() => regexCount(sequences, "agggta[cgt]a|t[acg]taccct", options));
#endif

            Task.WaitAll(variant1, variant2, variant3, variant4, variant5, variant6, variant7, variant8, variant9);

            return magicTask.Result;
        }

        private static string regexCount(string s, string r, RegexOptions regexOptions)
        {
            int c = 0;
            var m = Regex.Match(s, r, regexOptions);
            while (m.Success) { c++; m = m.NextMatch(); }
            return r + " " + c;
        }
    }
}
