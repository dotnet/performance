// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.RegularExpressions.Tests
{
    /// <summary>
    /// Performance tests for regex alternation patterns on non-ASCII text (Russian).
    /// </summary>
    [BenchmarkCategory(Categories.Libraries, Categories.Regex, Categories.NoWASM)]
    public class Perf_Regex_LeadingStrings_NonAscii
    {
        [Params(
            RegexOptions.None,
            RegexOptions.Compiled
#if NET7_0_OR_GREATER
            , RegexOptions.NonBacktracking
#endif
            )]
        public RegexOptions Options { get; set; }

        private string _input;
        private Regex _alternation;
        private Regex _alternationIgnoreCase;

        // Opening of Anna Karenina (Tolstoy), ~1KB of natural Russian prose.
        // "All happy families are alike; each unhappy family is unhappy in its own way.
        //  Everything was in confusion in the Oblonskys' house. The wife had discovered that the husband
        //  was carrying on an intrigue with their former French governess, and she had announced to her
        //  husband that she could not go on living in the same house with him..."
        private const string Text =
            "Все счастливые семьи похожи друг на друга, каждая несчастливая семья несчастлива по-своему. " +
            "Всё смешалось в доме Облонских. Жена узнала, что муж был в связи с бывшею в их доме француженкою-гувернанткой, " +
            "и объявила мужу, что не может жить с ним в одном доме. Положение это продолжалось уже третий день и чувствовалось " +
            "и самими супругами, и всеми членами семьи, и домочадцами. Все члены семьи и домочадцы чувствовали, что нет смысла " +
            "в их сожительстве и что на каждом постоялом дворе случайно сошедшиеся люди более связаны между собой, чем они, " +
            "члены семьи и домочадцы Облонских. Жена не выходила из своих комнат, мужа третий день не было дома. " +
            "Дети бегали по всему дому, как потерянные; англичанка поссорилась с экономкой и написала записку приятельнице, " +
            "прося приискать ей новое место; повар ушёл ещё вчера со двора, во время обеда; чёрная кухарка и кучер просили расчёт.";

        [GlobalSetup]
        public void Setup()
        {
            var sb = new System.Text.StringBuilder(100000);
            for (int i = 0; i < 100; i++)
                sb.Append(Text);
            _input = sb.ToString();

            _alternation = new Regex("семьи|Облонских|француженкою|домочадцами|сожительстве|англичанка|экономкой|кухарка", Options);
            _alternationIgnoreCase = new Regex("(?i)семьи|Облонских|француженкою|домочадцами|сожительстве|англичанка|экономкой|кухарка", Options);
        }

        [Benchmark]
        [MemoryRandomization]
        public int Count() => Perf_Regex_Industry.Count(_alternation, _input);

        [Benchmark]
        [MemoryRandomization]
        public int CountIgnoreCase() => Perf_Regex_Industry.Count(_alternationIgnoreCase, _input);
    }
}
