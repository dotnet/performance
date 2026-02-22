// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.RegularExpressions.Tests
{
    /// <summary>
    /// Performance tests for regex alternation patterns on binary data.
    /// Exercises the LeadingStrings vs FixedDistanceSets heuristic on non-text input.
    /// </summary>
    [BenchmarkCategory(Categories.Libraries, Categories.Regex, Categories.NoWASM)]
    public class Perf_Regex_LeadingStrings_BinaryData
    {
        [Params(
            RegexOptions.None,
            RegexOptions.Compiled
#if NET7_0_OR_GREATER
            , RegexOptions.NonBacktracking
#endif
            )]
        public RegexOptions Options { get; set; }

        private string _binaryText;
        private Regex _regex;

        // A small embedded binary-like fragment (~64 bytes) containing null bytes and typical PE patterns,
        // duplicated in Setup to create a ~1MB corpus. Using a fixed seed ensures identical input across TFMs.
        private static readonly byte[] s_binarySeed =
        {
            0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00,
            0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x50, 0x45, 0x00, 0x00, 0x64, 0x86, 0x02, 0x00, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x00, 0x00,
            0x2E, 0x74, 0x65, 0x78, 0x74, 0x00, 0x00, 0x00, 0x2E, 0x72, 0x73, 0x72, 0x63, 0x00, 0x00, 0x00,
        };

        [GlobalSetup]
        public void Setup()
        {
            // Duplicate the seed to ~1MB
            const int targetSize = 1024 * 1024;
            char[] chars = new char[targetSize];
            for (int i = 0; i < targetSize; i++)
                chars[i] = (char)s_binarySeed[i % s_binarySeed.Length];
            _binaryText = new string(chars);

            // Alternation of 4-byte sequences that appear in the seed (non-null starting)
            _regex = new Regex(@"MZ\x90\x00|PE\x00\x00|\.text|\.rsrc|Hello|d\x86\x02\x00", Options);
        }

        [Benchmark]
        [MemoryRandomization]
        public int Count() => Perf_Regex_Industry.Count(_regex, _binaryText);
    }

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
