// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Adapted from regex-redux C# .NET Core program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=regexredux&lang=csharpcore&id=1
// aka (as of 2017-09-01) rev 1.3 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/regexredux/regexredux.csharp?root=benchmarksgame&view=log
// Best-scoring single-threaded C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 * 
 * regex-dna program contributed by Isaac Gouy 
 * converted from regex-dna program
 *
*/

using System.IO;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace BenchmarksGame
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.BenchmarksGame)]
    public class RegexRedux_1
    {
        RegexReduxHelpers helpers = new RegexReduxHelpers(bigInput: true);
        
        [Benchmark(Description = nameof(RegexRedux_1))]
        public int RunBench()
        {
            using (var inputStream = new FileStream(helpers.InputFile, FileMode.Open))
            using (var input = new StreamReader(inputStream))
            {
                return Bench(input);
            }
        }

        static int Bench(TextReader inputReader)
        {
            // read FASTA sequence
            string sequence = inputReader.ReadToEnd();

            // remove FASTA sequence descriptions and new-lines
            Regex r = new Regex(">.*\n|\n", RegexOptions.Compiled);
            sequence = r.Replace(sequence, "");

            // regex match
            string[] variants = {
                "agggtaaa|tttaccct",
                "[cgt]gggtaaa|tttaccc[acg]",
                "a[act]ggtaaa|tttacc[agt]t",
                "ag[act]gtaaa|tttac[agt]ct",
                "agg[act]taaa|ttta[agt]cct",
                "aggg[acg]aaa|ttt[cgt]ccct",
                "agggt[cgt]aa|tt[acg]accct",
                "agggta[cgt]a|t[acg]taccct",
                "agggtaa[cgt]|[acg]ttaccct"
            };

            int count;
            foreach (string v in variants)
            {
                count = 0;
                r = new Regex(v, RegexOptions.Compiled);

                for (Match m = r.Match(sequence); m.Success; m = m.NextMatch()) count++;
            }

            // regex substitution
            IUB[] codes = {
                new IUB("tHa[Nt]", "<4>"),
                new IUB("aND|caN|Ha[DS]|WaS", "<3>"),
                new IUB("a[NSt]|BY", "<2>"),
                new IUB("<[^>]*>", "|"),
                new IUB("\\|[^|][^|]*\\|" , "-")
            };

            foreach (IUB iub in codes)
            {
                r = new Regex(iub.code, RegexOptions.Compiled);
                sequence = r.Replace(sequence, iub.alternatives);
            }

            return sequence.Length;
        }

        struct IUB
        {
            public string code;
            public string alternatives;

            public IUB(string code, string alternatives)
            {
                this.code = code;
                this.alternatives = alternatives;
            }
        }
    }
}
