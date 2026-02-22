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
    public class Perf_Regex_Industry_BinaryData
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
    /// Performance tests for regex alternation patterns on non-ASCII (multilingual) text.
    /// </summary>
    [BenchmarkCategory(Categories.Libraries, Categories.Regex, Categories.NoWASM)]
    public class Perf_Regex_Industry_NonAscii
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

        // ~12KB of mixed-script text: Cyrillic, CJK, Arabic, Devanagari, Latin with diacritics.
        private const string CyrillicText =
            "Все счастливые семьи похожи друг на друга, каждая несчастливая семья несчастлива по-своему. " +
            "Всё смешалось в доме Облонских. Жена узнала, что муж был в связи с бывшею в их доме француженкою-гувернанткой, " +
            "и объявила мужу, что не может жить с ним в одном доме. Положение это продолжалось уже третий день и чувствовалось " +
            "и самими супругами, и всеми членами семьи, и домочадцами. ";

        private const string CjkText =
            "国破山河在城春草木深感时花溅泪恨别鸟惊心烽火连三月家书抵万金白头搔更短浑欲不胜簪" +
            "春望杜甫国破山河在城春草木深感时花溅泪恨别鸟惊心烽火连三月家书抵万金白头搔更短浑欲不胜簪" +
            "床前明月光疑是地上霜举头望明月低头思故乡静夜思李白床前明月光疑是地上霜举头望明月低头思故乡";

        private const string ArabicText =
            "حكي أنه كان في قديم الزمان وسالف العصر والأوان تاجر من التجار كثير المال والأعمال " +
            "وكان له أولاد وعيال وكان قد أعطاه الله سبحانه وتعالى من الأموال والأرزاق والمتاجر في سائر البلاد";

        private const string DevanagariText =
            "धर्मक्षेत्रे कुरुक्षेत्रे समवेता युयुत्सवः मामकाः पाण्डवाश्चैव किमकुर्वत सञ्जय " +
            "दृष्ट्वा तु पाण्डवानीकं व्यूढं दुर्योधनस्तदा आचार्यमुपसंगम्य राजा वचनमब्रवीत्";

        private const string LatinDiacriticsText =
            "Dès Noël où un zéphyr haï me vêt de glaçons würmiens, je dîne d'exquis rôtis de bœuf au kir à l'aÿ d'âge mûr & cætera. " +
            "El veloz murciélago hindú comía feliz cardillo y kiwi. La cigüeña tocaba el saxofón detrás del palenque de paja. " +
            "Příliš žluťoučký kůň úpěl ďábelské ódy. Høj bansen flyver over det røde hus med æbler og ål.";

        [GlobalSetup]
        public void Setup()
        {
            // Build a substantial input by repeating the multilingual text
            var sb = new System.Text.StringBuilder(50000);
            for (int i = 0; i < 50; i++)
            {
                sb.Append(CyrillicText);
                sb.Append(CjkText);
                sb.Append(ArabicText);
                sb.Append(DevanagariText);
                sb.Append(LatinDiacriticsText);
            }
            _input = sb.ToString();

            // Alternation of words from different scripts
            _alternation = new Regex("семьи|花溅泪|الرحمن|कुरुक्षेत्रे|cigüeña|zéphyr|murciélago|würmiens", Options);
            _alternationIgnoreCase = new Regex("(?i)семьи|花溅泪|الرحمن|कुरुक्षेत्रे|cigüeña|zéphyr|murciélago|würmiens", Options);
        }

        [Benchmark]
        [MemoryRandomization]
        public int Count() => Perf_Regex_Industry.Count(_alternation, _input);

        [Benchmark]
        [MemoryRandomization]
        public int CountIgnoreCase() => Perf_Regex_Industry.Count(_alternationIgnoreCase, _input);
    }
}
