// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.RegularExpressions.Tests
{
    /// <summary>
    /// Performance tests for Regular Expressions
    /// </summary>
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Regex
    {
        private static readonly (string pattern, string input, RegexOptions options)[] TestData = Match_TestData().ToArray();

        private int _cacheSizeOld;

        [GlobalSetup]
        public void Setup()
        {
            _cacheSizeOld = Regex.CacheSize;
            Regex.CacheSize = 0; // disable cache to get clearer results
        }
        
        [GlobalCleanup]
        public void Cleanup() => Regex.CacheSize = _cacheSizeOld;

        [Benchmark]
        public bool Match()
        {
            bool result = false;
            
            foreach ((string pattern, string input, RegexOptions options) test in TestData)
                result ^= Regex.Match(test.input, test.pattern, test.options).Success;

            return result;
        }

        [Benchmark]
        public bool IsMatch()
        {
            bool result = false;

            foreach ((string pattern, string input, RegexOptions options) test in TestData)
                result ^= Regex.IsMatch(test.input, test.pattern, test.options);

            return result;
        }

        // A series of patterns (all valid and non pathological) and inputs (which they may or may not match)
        public static IEnumerable<(string, string, RegexOptions)> Match_TestData()
        {
            yield return ("[abcd-[d]]+", "dddaabbccddd", RegexOptions.None);
            yield return (@"[\d-[357]]+", "33312468955", RegexOptions.None);
            yield return (@"[\d-[357]]+", "51246897", RegexOptions.None);
            yield return (@"[\d-[357]]+", "3312468977", RegexOptions.None);
            yield return (@"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None);
            yield return (@"[\w-[\d]]+", "0AZaz9", RegexOptions.None);
            yield return (@"[\w-[\p{Ll}]]+", "a09AZz", RegexOptions.None);
            yield return (@"[\d-[13579]]+", "1024689", RegexOptions.ECMAScript);
            yield return (@"[\d-[13579]]+", "\x066102468\x0660", RegexOptions.ECMAScript);
            yield return (@"[\d-[13579]]+", "\x066102468\x0660", RegexOptions.None);
            yield return (@"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None);
            yield return (@"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None);
            yield return (@"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None);
            yield return (@"[\p{Ll}-[ae-z]]+", "aaabbbcccdddeee", RegexOptions.None);
            yield return (@"[\p{Nd}-[2468]]+", "20135798", RegexOptions.None);
            yield return (@"[\P{Lu}-[ae-z]]+", "aaabbbcccdddeee", RegexOptions.None);
            yield return (@"[\P{Nd}-[\p{Ll}]]+", "az09AZ'[]", RegexOptions.None);
            yield return ("[abcd-[def]]+", "fedddaabbccddd", RegexOptions.None);
            yield return (@"[\d-[357a-z]]+", "az33312468955", RegexOptions.None);
            yield return (@"[\d-[de357fgA-Z]]+", "AZ51246897", RegexOptions.None);
            yield return (@"[\d-[357\p{Ll}]]+", "az3312468977", RegexOptions.None);
            yield return (@"[\w-[b-y\s]]+", " \tbbbaaaABCD09zzzyyy", RegexOptions.None);
            yield return (@"[\w-[\d\p{Po}]]+", "!#0AZaz9", RegexOptions.None);
            yield return (@"[\w-[\p{Ll}\s]]+", "a09AZz", RegexOptions.None);
            yield return (@"[\d-[13579a-zA-Z]]+", "AZ1024689", RegexOptions.ECMAScript);
            yield return (@"[\d-[13579abcd]]+", "abcd\x066102468\x0660", RegexOptions.ECMAScript);
            yield return (@"[\d-[13579\s]]+", " \t\x066102468\x0660", RegexOptions.None);
            yield return (@"[\w-[b-y\p{Po}]]+", "!#bbbaaaABCD09zzzyyy", RegexOptions.None);
            yield return (@"[\w-[b-y!.,]]+", "!.,bbbaaaABCD09zzzyyy", RegexOptions.None);
            yield return ("[\\w-[b-y\x00-\x0F]]+", "\0bbbaaaABCD09zzzyyy", RegexOptions.None);
            yield return (@"[\p{Ll}-[ae-z0-9]]+", "09aaabbbcccdddeee", RegexOptions.None);
            yield return (@"[\p{Nd}-[2468az]]+", "az20135798", RegexOptions.None);
            yield return (@"[\P{Lu}-[ae-zA-Z]]+", "AZaaabbbcccdddeee", RegexOptions.None);
            yield return (@"[\P{Nd}-[\p{Ll}0123456789]]+", "09az09AZ'[]", RegexOptions.None);
            yield return ("[abc-[defg]]+", "dddaabbccddd", RegexOptions.None);
            yield return (@"[\d-[abc]]+", "abc09abc", RegexOptions.None);
            yield return (@"[\d-[a-zA-Z]]+", "az09AZ", RegexOptions.None);
            yield return (@"[\d-[\p{Ll}]]+", "az09az", RegexOptions.None);
            yield return (@"[\w-[\x00-\x0F]]+", "bbbaaaABYZ09zzzyyy", RegexOptions.None);
            yield return (@"[\w-[\s]]+", "0AZaz9", RegexOptions.None);
            yield return (@"[\w-[\W]]+", "0AZaz9", RegexOptions.None);
            yield return (@"[\w-[\p{Po}]]+", "#a09AZz!", RegexOptions.None);
            yield return (@"[\d-[\D]]+", "azAZ1024689", RegexOptions.ECMAScript);
            yield return (@"[\d-[a-zA-Z]]+", "azAZ\x066102468\x0660", RegexOptions.ECMAScript);
            yield return (@"[\d-[\p{Ll}]]+", "\x066102468\x0660", RegexOptions.None);
            yield return (@"[a-zA-Z0-9-[\s]]+", " \tazAZ09", RegexOptions.None);
            yield return (@"[a-zA-Z0-9-[\W]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None);
            yield return (@"[a-zA-Z0-9-[^a-zA-Z0-9]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None);
            yield return (@"[\p{Ll}-[A-Z]]+", "AZaz09", RegexOptions.None);
            yield return (@"[\p{Nd}-[a-z]]+", "az09", RegexOptions.None);
            yield return (@"[\P{Lu}-[\p{Lu}]]+", "AZazAZ", RegexOptions.None);
            yield return (@"[\P{Lu}-[A-Z]]+", "AZazAZ", RegexOptions.None);
            yield return (@"[\P{Nd}-[\p{Nd}]]+", "azAZ09", RegexOptions.None);
            yield return (@"[\P{Nd}-[2-8]]+", "1234567890azAZ1234567890", RegexOptions.None);
            yield return (@"([ ]|[\w-[0-9]])+", "09az AZ90", RegexOptions.None);
            yield return (@"([0-9-[02468]]|[0-9-[13579]])+", "az1234567890za", RegexOptions.None);
            yield return (@"([^0-9-[a-zAE-Z]]|[\w-[a-zAF-Z]])+", "azBCDE1234567890BCDEFza", RegexOptions.None);
            yield return (@"([\p{Ll}-[aeiou]]|[^\w-[\s]])+", "aeiobcdxyz!@#aeio", RegexOptions.None);
            yield return (@"98[\d-[9]][\d-[8]][\d-[0]]", "98911 98881 98870 98871", RegexOptions.None);
            yield return (@"m[\w-[^aeiou]][\w-[^aeiou]]t", "mbbt mect meet", RegexOptions.None);
            yield return ("[abcdef-[^bce]]+", "adfbcefda", RegexOptions.None);
            yield return ("[^cde-[ag]]+", "agbfxyzga", RegexOptions.None);
            yield return (@"[\p{L}-[^\p{Lu}]]+", "09',.abcxyzABCXYZ", RegexOptions.None);
            yield return (@"[\p{IsGreek}-[\P{Lu}]]+", "\u0390\u03FE\u0386\u0388\u03EC\u03EE\u0400", RegexOptions.None);
            yield return (@"[\p{IsBasicLatin}-[G-L]]+", "GAFMZL", RegexOptions.None);
            yield return ("[a-zA-Z-[aeiouAEIOU]]+", "aeiouAEIOUbcdfghjklmnpqrstvwxyz", RegexOptions.None);
            yield return (@"^
            (?<octet>^
                (
                    (
                        (?<Octet2xx>[\d-[013-9]])
                        |
                        [\d-[2-9]]
                    )
                    (?(Octet2xx)
                        (
                            (?<Octet25x>[\d-[01-46-9]])
                            |
                            [\d-[5-9]]
                        )
                        (
                            (?(Octet25x)
                                [\d-[6-9]]
                                |
                                [\d]
                            )
                        )
                        |
                        [\d]{2}
                    )
                )
                |
                ([\d][\d])
                |
                [\d]
            )$"
            , "255", RegexOptions.IgnorePatternWhitespace);
            yield return (@"[abcd\-d-[bc]]+", "bbbaaa---dddccc", RegexOptions.None);
            yield return (@"[abcd\-d-[bc]]+", "bbbaaa---dddccc", RegexOptions.None);
            yield return (@"[^a-f-[\x00-\x60\u007B-\uFFFF]]+", "aaafffgggzzz{{{", RegexOptions.None);
            yield return (@"[\[\]a-f-[[]]+", "gggaaafff]]][[[", RegexOptions.None);
            yield return (@"[\[\]a-f-[]]]+", "gggaaafff[[[]]]", RegexOptions.None);
            yield return (@"[ab\-\[cd-[-[]]]]", "a]]", RegexOptions.None);
            yield return (@"[ab\-\[cd-[-[]]]]", "b]]", RegexOptions.None);
            yield return (@"[ab\-\[cd-[-[]]]]", "c]]", RegexOptions.None);
            yield return (@"[ab\-\[cd-[-[]]]]", "d]]", RegexOptions.None);
            yield return (@"[ab\-\[cd-[[]]]]", "a]]", RegexOptions.None);
            yield return (@"[ab\-\[cd-[[]]]]", "b]]", RegexOptions.None);
            yield return (@"[ab\-\[cd-[[]]]]", "c]]", RegexOptions.None);
            yield return (@"[ab\-\[cd-[[]]]]", "d]]", RegexOptions.None);
            yield return (@"[ab\-\[cd-[[]]]]", "-]]", RegexOptions.None);
            yield return (@"[a-[c-e]]+", "bbbaaaccc", RegexOptions.None);
            yield return (@"[a-[c-e]]+", "```aaaccc", RegexOptions.None);
            yield return (@"[a-d\--[bc]]+", "cccaaa--dddbbb", RegexOptions.None);
            yield return (@"[\0- [bc]+", "!!!\0\0\t\t  [[[[bbbcccaaa", RegexOptions.None);
            yield return ("[[abcd]-[bc]]+", "a-b]", RegexOptions.None);
            yield return ("[-[e-g]+", "ddd[[[---eeefffggghhh", RegexOptions.None);
            yield return ("[-e-g]+", "ddd---eeefffggghhh", RegexOptions.None);
            yield return ("[-e-g]+", "ddd---eeefffggghhh", RegexOptions.None);
            yield return ("[a-e - m-p]+", "---a b c d e m n o p---", RegexOptions.None);
            yield return ("[^-[bc]]", "b] c] -] aaaddd]", RegexOptions.None);
            yield return ("[^-[bc]]", "b] c] -] aaa]ddd]", RegexOptions.None);
            yield return (@"[a\-[bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None);
            yield return (@"[a\-[\-\-bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None);
            yield return (@"[a\-\[\-\[\-bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None);
            yield return (@"[abc\--[b]]+", "[[[```bbbaaa---cccddd", RegexOptions.None);
            yield return (@"[abc\-z-[b]]+", "```aaaccc---zzzbbb", RegexOptions.None);
            yield return (@"[a-d\-[b]+", "```aaabbbcccddd----[[[[]]]", RegexOptions.None);
            yield return (@"[abcd\-d\-[bc]+", "bbbaaa---[[[dddccc", RegexOptions.None);
            yield return ("[a - c - [ b ] ]+", "dddaaa   ccc [[[[ bbb ]]]", RegexOptions.IgnorePatternWhitespace);
            yield return ("[a - c - [ b ] +", "dddaaa   ccc [[[[ bbb ]]]", RegexOptions.IgnorePatternWhitespace);
            yield return (@"(\p{Lu}\w*)\s(\p{Lu}\w*)", "Hello World", RegexOptions.None);
            yield return (@"(\p{Lu}\p{Ll}*)\s(\p{Lu}\p{Ll}*)", "Hello World", RegexOptions.None);
            yield return (@"(\P{Ll}\p{Ll}*)\s(\P{Ll}\p{Ll}*)", "Hello World", RegexOptions.None);
            yield return (@"(\P{Lu}+\p{Lu})\s(\P{Lu}+\p{Lu})", "hellO worlD", RegexOptions.None);
            yield return (@"(\p{Lt}\w*)\s(\p{Lt}*\w*)", "\u01C5ello \u01C5orld", RegexOptions.None);
            yield return (@"(\P{Lt}\w*)\s(\P{Lt}*\w*)", "Hello World", RegexOptions.None);
            yield return (@"[@-D]+", "eE?@ABCDabcdeE", RegexOptions.IgnoreCase);
            yield return (@"[>-D]+", "eE=>?@ABCDabcdeE", RegexOptions.IgnoreCase);
            yield return (@"[\u0554-\u0557]+", "\u0583\u0553\u0554\u0555\u0556\u0584\u0585\u0586\u0557\u0558", RegexOptions.IgnoreCase);
            yield return (@"[X-\]]+", "wWXYZxyz[\\]^", RegexOptions.IgnoreCase);
            yield return (@"[X-\u0533]+", "\u0551\u0554\u0560AXYZaxyz\u0531\u0532\u0533\u0561\u0562\u0563\u0564", RegexOptions.IgnoreCase);
            yield return (@"[X-a]+", "wWAXYZaxyz", RegexOptions.IgnoreCase);
            yield return (@"[X-c]+", "wWABCXYZabcxyz", RegexOptions.IgnoreCase);
            yield return (@"[X-\u00C0]+", "\u00C1\u00E1\u00C0\u00E0wWABCXYZabcxyz", RegexOptions.IgnoreCase);
            yield return (@"[\u0100\u0102\u0104]+", "\u00FF \u0100\u0102\u0104\u0101\u0103\u0105\u0106", RegexOptions.IgnoreCase);
            yield return (@"[B-D\u0130]+", "aAeE\u0129\u0131\u0068 BCDbcD\u0130\u0069\u0070", RegexOptions.IgnoreCase);
            yield return (@"[\u013B\u013D\u013F]+", "\u013A\u013B\u013D\u013F\u013C\u013E\u0140\u0141", RegexOptions.IgnoreCase);
            yield return ("(Cat)\r(Dog)", "Cat\rDog", RegexOptions.None);
            yield return ("(Cat)\t(Dog)", "Cat\tDog", RegexOptions.None);
            yield return ("(Cat)\f(Dog)", "Cat\fDog", RegexOptions.None);
            yield return (@"{5", "hello {5 world", RegexOptions.None);
            yield return (@"{5,", "hello {5, world", RegexOptions.None);
            yield return (@"{5,6", "hello {5,6 world", RegexOptions.None);
            yield return (@"(?n:(?<cat>cat)(\s+)(?<dog>dog))", "cat   dog", RegexOptions.None);
            yield return (@"(?n:(cat)(\s+)(dog))", "cat   dog", RegexOptions.None);
            yield return (@"(?n:(cat)(?<SpaceChars>\s+)(dog))", "cat   dog", RegexOptions.None);
            yield return (@"(?x:
                            (?<cat>cat) # Cat statement
                            (\s+) # Whitespace chars
                            (?<dog>dog # Dog statement
                            ))", "cat   dog", RegexOptions.None);
            yield return (@"(?+i:cat)", "CAT", RegexOptions.None);
            yield return (@"cat([\d]*)dog", "hello123cat230927dog1412d", RegexOptions.None);
            yield return (@"([\D]*)dog", "65498catdog58719", RegexOptions.None);
            yield return (@"cat([\s]*)dog", "wiocat   dog3270", RegexOptions.None);
            yield return (@"cat([\S]*)", "sfdcatdog    3270", RegexOptions.None);
            yield return (@"cat([\w]*)", "sfdcatdog    3270", RegexOptions.None);
            yield return (@"cat([\W]*)dog", "wiocat   dog3270", RegexOptions.None);
            yield return (@"([\p{Lu}]\w*)\s([\p{Lu}]\w*)", "Hello World", RegexOptions.None);
            yield return (@"([\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", "Hello World", RegexOptions.None);
            yield return (@"(cat)([\x41]*)(dog)", "catAAAdog", RegexOptions.None);
            yield return (@"(cat)([\u0041]*)(dog)", "catAAAdog", RegexOptions.None);
            yield return (@"(cat)([\a]*)(dog)", "cat\a\a\adog", RegexOptions.None);
            yield return (@"(cat)([\b]*)(dog)", "cat\b\b\bdog", RegexOptions.None);
            yield return (@"(cat)([\e]*)(dog)", "cat\u001B\u001B\u001Bdog", RegexOptions.None);
            yield return (@"(cat)([\f]*)(dog)", "cat\f\f\fdog", RegexOptions.None);
            yield return (@"(cat)([\r]*)(dog)", "cat\r\r\rdog", RegexOptions.None);
            yield return (@"(cat)([\v]*)(dog)", "cat\v\v\vdog", RegexOptions.None);
            yield return (@"cat([\d]*)dog", "hello123cat230927dog1412d", RegexOptions.ECMAScript);
            yield return (@"([\D]*)dog", "65498catdog58719", RegexOptions.ECMAScript);
            yield return (@"cat([\s]*)dog", "wiocat   dog3270", RegexOptions.ECMAScript);
            yield return (@"cat([\S]*)", "sfdcatdog    3270", RegexOptions.ECMAScript);
            yield return (@"cat([\w]*)", "sfdcatdog    3270", RegexOptions.ECMAScript);
            yield return (@"cat([\W]*)dog", "wiocat   dog3270", RegexOptions.ECMAScript);
            yield return (@"([\p{Lu}]\w*)\s([\p{Lu}]\w*)", "Hello World", RegexOptions.ECMAScript);
            yield return (@"([\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", "Hello World", RegexOptions.ECMAScript);
            yield return (@"(cat)\d*dog", "hello123cat230927dog1412d", RegexOptions.ECMAScript);
            yield return (@"\D*(dog)", "65498catdog58719", RegexOptions.ECMAScript);
            yield return (@"(cat)\s*(dog)", "wiocat   dog3270", RegexOptions.ECMAScript);
            yield return (@"(cat)\S*", "sfdcatdog    3270", RegexOptions.ECMAScript);
            yield return (@"(cat)\w*", "sfdcatdog    3270", RegexOptions.ECMAScript);
            yield return (@"(cat)\W*(dog)", "wiocat   dog3270", RegexOptions.ECMAScript);
            yield return (@"\p{Lu}(\w*)\s\p{Lu}(\w*)", "Hello World", RegexOptions.ECMAScript);
            yield return (@"\P{Ll}\p{Ll}*\s\P{Ll}\p{Ll}*", "Hello World", RegexOptions.ECMAScript);
            yield return (@"cat(?<dog121>dog)", "catcatdogdogcat", RegexOptions.None);
            yield return (@"(?<cat>cat)\s*(?<cat>dog)", "catcat    dogdogcat", RegexOptions.None);
            yield return (@"(?<1>cat)\s*(?<1>dog)", "catcat    dogdogcat", RegexOptions.None);
            yield return (@"(?<2048>cat)\s*(?<2048>dog)", "catcat    dogdogcat", RegexOptions.None);
            yield return (@"(?<cat>cat)\w+(?<dog-cat>dog)", "cat_Hello_World_dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\w+(?<-cat>dog)", "cat_Hello_World_dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\w+(?<cat-cat>dog)", "cat_Hello_World_dog", RegexOptions.None);
            yield return (@"(?<1>cat)\w+(?<dog-1>dog)", "cat_Hello_World_dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\w+(?<2-cat>dog)", "cat_Hello_World_dog", RegexOptions.None);
            yield return (@"(?<1>cat)\w+(?<2-1>dog)", "cat_Hello_World_dog", RegexOptions.None);
            yield return (@"(?<cat>cat){", "STARTcat{", RegexOptions.None);
            yield return (@"(?<cat>cat){fdsa", "STARTcat{fdsa", RegexOptions.None);
            yield return (@"(?<cat>cat){1", "STARTcat{1", RegexOptions.None);
            yield return (@"(?<cat>cat){1END", "STARTcat{1END", RegexOptions.None);
            yield return (@"(?<cat>cat){1,", "STARTcat{1,", RegexOptions.None);
            yield return (@"(?<cat>cat){1,END", "STARTcat{1,END", RegexOptions.None);
            yield return (@"(?<cat>cat){1,2", "STARTcat{1,2", RegexOptions.None);
            yield return (@"(?<cat>cat){1,2END", "STARTcat{1,2END", RegexOptions.None);
            yield return (@"(cat) #cat
                            \s+ #followed by 1 or more whitespace
                            (dog)  #followed by dog
                            ", "cat    dog", RegexOptions.IgnorePatternWhitespace);
            yield return (@"(cat) #cat
                            \s+ #followed by 1 or more whitespace
                            (dog)  #followed by dog", "cat    dog", RegexOptions.IgnorePatternWhitespace);
            yield return (@"(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace) (dog)  (?#followed by dog)", "cat    dog", RegexOptions.IgnorePatternWhitespace);
            yield return (@"(?<cat>cat)(?<dog>dog)\k<cat>", "asdfcatdogcatdog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\k<cat>", "asdfcat   dogcat   dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\k'cat'", "asdfcat   dogcat   dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\<cat>", "asdfcat   dogcat   dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\'cat'", "asdfcat   dogcat   dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\k<1>", "asdfcat   dogcat   dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\k'1'", "asdfcat   dogcat   dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\<1>", "asdfcat   dogcat   dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\'1'", "asdfcat   dogcat   dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\1", "asdfcat   dogcat   dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\1", "asdfcat   dogcat   dog", RegexOptions.ECMAScript);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\k<dog>", "asdfcat   dogdog   dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\2", "asdfcat   dogdog   dog", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\2", "asdfcat   dogdog   dog", RegexOptions.ECMAScript);
            yield return (@"(cat)(\077)", "hellocat?dogworld", RegexOptions.None);
            yield return (@"(cat)(\77)", "hellocat?dogworld", RegexOptions.None);
            yield return (@"(cat)(\176)", "hellocat~dogworld", RegexOptions.None);
            yield return (@"(cat)(\400)", "hellocat\0dogworld", RegexOptions.None);
            yield return (@"(cat)(\300)", "hellocat\u00C0dogworld", RegexOptions.None);
            yield return (@"(cat)(\300)", "hellocat\u00C0dogworld", RegexOptions.None);
            yield return (@"(cat)(\477)", "hellocat\u003Fdogworld", RegexOptions.None);
            yield return (@"(cat)(\777)", "hellocat\u00FFdogworld", RegexOptions.None);
            yield return (@"(cat)(\7770)", "hellocat\u00FF0dogworld", RegexOptions.None);
            yield return (@"(cat)(\077)", "hellocat?dogworld", RegexOptions.ECMAScript);
            yield return (@"(cat)(\77)", "hellocat?dogworld", RegexOptions.ECMAScript);
            yield return (@"(cat)(\7)", "hellocat\adogworld", RegexOptions.ECMAScript);
            yield return (@"(cat)(\40)", "hellocat dogworld", RegexOptions.ECMAScript);
            yield return (@"(cat)(\040)", "hellocat dogworld", RegexOptions.ECMAScript);
            yield return (@"(cat)(\176)", "hellocatcat76dogworld", RegexOptions.ECMAScript);
            yield return (@"(cat)(\377)", "hellocat\u00FFdogworld", RegexOptions.ECMAScript);
            yield return (@"(cat)(\400)", "hellocat 0Fdogworld", RegexOptions.ECMAScript);
            yield return (@"(cat)\s+(?<2147483646>dog)", "asdlkcat  dogiwod", RegexOptions.None);
            yield return (@"(cat)\s+(?<2147483647>dog)", "asdlkcat  dogiwod", RegexOptions.None);
            yield return (@"(cat)(\x2a*)(dog)", "asdlkcat***dogiwod", RegexOptions.None);
            yield return (@"(cat)(\x2b*)(dog)", "asdlkcat+++dogiwod", RegexOptions.None);
            yield return (@"(cat)(\x2c*)(dog)", "asdlkcat,,,dogiwod", RegexOptions.None);
            yield return (@"(cat)(\x2d*)(dog)", "asdlkcat---dogiwod", RegexOptions.None);
            yield return (@"(cat)(\x2e*)(dog)", "asdlkcat...dogiwod", RegexOptions.None);
            yield return (@"(cat)(\x2A*)(dog)", "asdlkcat***dogiwod", RegexOptions.None);
            yield return (@"(cat)(\x2B*)(dog)", "asdlkcat+++dogiwod", RegexOptions.None);
            yield return (@"(cat)(\x2C*)(dog)", "asdlkcat,,,dogiwod", RegexOptions.None);
            yield return (@"(cat)(\x2D*)(dog)", "asdlkcat---dogiwod", RegexOptions.None);
            yield return (@"(cat)(\x2E*)(dog)", "asdlkcat...dogiwod", RegexOptions.None);
            yield return (@"(cat)(\c@*)(dog)", "asdlkcat\0\0dogiwod", RegexOptions.None);
            yield return (@"(cat)(\cA*)(dog)", "asdlkcat\u0001dogiwod", RegexOptions.None);
            yield return (@"(cat)(\ca*)(dog)", "asdlkcat\u0001dogiwod", RegexOptions.None);
            yield return (@"(cat)(\cC*)(dog)", "asdlkcat\u0003dogiwod", RegexOptions.None);
            yield return (@"(cat)(\cc*)(dog)", "asdlkcat\u0003dogiwod", RegexOptions.None);
            yield return (@"(cat)(\cD*)(dog)", "asdlkcat\u0004dogiwod", RegexOptions.None);
            yield return (@"(cat)(\cd*)(dog)", "asdlkcat\u0004dogiwod", RegexOptions.None);
            yield return (@"(cat)(\cX*)(dog)", "asdlkcat\u0018dogiwod", RegexOptions.None);
            yield return (@"(cat)(\cx*)(dog)", "asdlkcat\u0018dogiwod", RegexOptions.None);
            yield return (@"(cat)(\cZ*)(dog)", "asdlkcat\u001adogiwod", RegexOptions.None);
            yield return (@"(cat)(\cz*)(dog)", "asdlkcat\u001adogiwod", RegexOptions.None);
            yield return (@"\A(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.None);
            yield return (@"\A(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.Multiline);
            yield return (@"\A(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.ECMAScript);
            yield return (@"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.None);
            yield return (@"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.Multiline);
            yield return (@"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.ECMAScript);
            yield return (@"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.None);
            yield return (@"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.Multiline);
            yield return (@"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.ECMAScript);
            yield return (@"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.None);
            yield return (@"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.Multiline);
            yield return (@"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.ECMAScript);
            yield return (@"\b@cat", "123START123@catEND", RegexOptions.None);
            yield return (@"\b\<cat", "123START123<catEND", RegexOptions.None);
            yield return (@"\b,cat", "satwe,,,START,catEND", RegexOptions.None);
            yield return (@"\b\[cat", "`12START123[catEND", RegexOptions.None);
            yield return (@"\B@cat", "123START123;@catEND", RegexOptions.None);
            yield return (@"\B\<cat", "123START123'<catEND", RegexOptions.None);
            yield return (@"\B,cat", "satwe,,,START',catEND", RegexOptions.None);
            yield return (@"\B\[cat", "`12START123'[catEND", RegexOptions.None);
            yield return (@"(\w+)\s+(\w+)", "cat\u02b0 dog\u02b1", RegexOptions.None);
            yield return (@"(cat\w+)\s+(dog\w+)", "STARTcat\u30FC dog\u3005END", RegexOptions.None);
            yield return (@"(cat\w+)\s+(dog\w+)", "STARTcat\uff9e dog\uff9fEND", RegexOptions.None);
            yield return (@"[^a]|d", "d", RegexOptions.None);
            yield return (@"([^a]|[d])*", "Hello Worlddf", RegexOptions.None);
            yield return (@"([^{}]|\n)+", "{{{{Hello\n World \n}END", RegexOptions.None);
            yield return (@"([a-d]|[^abcd])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None);
            yield return (@"([^a]|[a])*", "once upon a time", RegexOptions.None);
            yield return (@"([a-d]|[^abcd]|[x-z]|^wxyz])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None);
            yield return (@"([a-d]|[e-i]|[^e]|wxyz])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None);
            yield return (@"^(([^b]+ )|(.* ))$", "aaa ", RegexOptions.None);
            yield return (@"^(([^b]+ )|(.*))$", "aaa", RegexOptions.None);
            yield return (@"^(([^b]+ )|(.* ))$", "bbb ", RegexOptions.None);
            yield return (@"^(([^b]+ )|(.*))$", "bbb", RegexOptions.None);
            yield return (@"^((a*)|(.*))$", "aaa", RegexOptions.None);
            yield return (@"^((a*)|(.*))$", "aaabbb", RegexOptions.None);
            yield return (@"(([0-9])|([a-z])|([A-Z]))*", "{hello 1234567890 world}", RegexOptions.None);
            yield return (@"(([0-9])|([a-z])|([A-Z]))+", "{hello 1234567890 world}", RegexOptions.None);
            yield return (@"(([0-9])|([a-z])|([A-Z]))*", "{HELLO 1234567890 world}", RegexOptions.None);
            yield return (@"(([0-9])|([a-z])|([A-Z]))+", "{HELLO 1234567890 world}", RegexOptions.None);
            yield return (@"(([0-9])|([a-z])|([A-Z]))*", "{1234567890 hello  world}", RegexOptions.None);
            yield return (@"(([0-9])|([a-z])|([A-Z]))+", "{1234567890 hello world}", RegexOptions.None);
            yield return (@"^(([a-d]*)|([a-z]*))$", "aaabbbcccdddeeefff", RegexOptions.None);
            yield return (@"^(([d-f]*)|([c-e]*))$", "dddeeeccceee", RegexOptions.None);
            yield return (@"^(([c-e]*)|([d-f]*))$", "dddeeeccceee", RegexOptions.None);
            yield return (@"(([a-d]*)|([a-z]*))", "aaabbbcccdddeeefff", RegexOptions.None);
            yield return (@"(([d-f]*)|([c-e]*))", "dddeeeccceee", RegexOptions.None);
            yield return (@"(([c-e]*)|([d-f]*))", "dddeeeccceee", RegexOptions.None);
            yield return (@"(([a-d]*)|(.*))", "aaabbbcccdddeeefff", RegexOptions.None);
            yield return (@"(([d-f]*)|(.*))", "dddeeeccceee", RegexOptions.None);
            yield return (@"(([c-e]*)|(.*))", "dddeeeccceee", RegexOptions.None);
            yield return (@"\p{Pi}(\w*)\p{Pf}", "\u00ABCat\u00BB   \u00BBDog\u00AB'", RegexOptions.None);
            yield return (@"\p{Pi}(\w*)\p{Pf}", "\u2018Cat\u2019   \u2019Dog\u2018'", RegexOptions.None);
            yield return (@"(?<cat>cat)\s+(?<dog>dog)\s+\123\s+\234", "asdfcat   dog     cat23    dog34eia", RegexOptions.ECMAScript);
            yield return (@"<div> 
            (?> 
                <div>(?<DEPTH>) |   
                </div> (?<-DEPTH>) |  
                .?
            )*?
            (?(DEPTH)(?!)) 
            </div>", "<div>this is some <div>red</div> text</div></div></div>", RegexOptions.IgnorePatternWhitespace);
            yield return (@"(
            ((?'open'<+)[^<>]*)+
            ((?'close-open'>+)[^<>]*)+
            )+", "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", RegexOptions.IgnorePatternWhitespace);
            yield return (@"(
            (?<start><)?
            [^<>]?
            (?<end-start>>)?
            )*", "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", RegexOptions.IgnorePatternWhitespace);
            yield return (@"(
            (?<start><[^/<>]*>)?
            [^<>]?
            (?<end-start></[^/<>]*>)?
            )*", "<b><a>Cat</a></b>", RegexOptions.IgnorePatternWhitespace);
            yield return (@"(
            (?<start><(?<TagName>[^/<>]*)>)?
            [^<>]?
            (?<end-start></\k<TagName>>)?
            )*", "<b>cat</b><a>dog</a>", RegexOptions.IgnorePatternWhitespace);
            yield return (@"([0-9]+?)([\w]+?)", "55488aheiaheiad", RegexOptions.ECMAScript);
            yield return (@"([0-9]+?)([a-z]+?)", "55488aheiaheiad", RegexOptions.ECMAScript);
            yield return (@"\G<%#(?<code>.*?)?%>", @"<%# DataBinder.Eval(this, ""MyNumber"") %>", RegexOptions.Singleline);
            yield return (@"^[abcd]{0,0x10}*$", "a{0,0x10}}}", RegexOptions.None);
            yield return (@"([a-z]*?)([\w])", "cat", RegexOptions.IgnoreCase);
            yield return (@"^([a-z]*?)([\w])$", "cat", RegexOptions.IgnoreCase);
            yield return (@"([a-z]*)([\w])", "cat", RegexOptions.IgnoreCase);
            yield return (@"^([a-z]*)([\w])$", "cat", RegexOptions.IgnoreCase);
            yield return (@"(cat){", "cat{", RegexOptions.None);
            yield return (@"(cat){}", "cat{}", RegexOptions.None);
            yield return (@"(cat){,", "cat{,", RegexOptions.None);
            yield return (@"(cat){,}", "cat{,}", RegexOptions.None);
            yield return (@"(cat){cat}", "cat{cat}", RegexOptions.None);
            yield return (@"(cat){cat,5}", "cat{cat,5}", RegexOptions.None);
            yield return (@"(cat){5,dog}", "cat{5,dog}", RegexOptions.None);
            yield return (@"(cat){cat,dog}", "cat{cat,dog}", RegexOptions.None);
            yield return (@"(cat){,}?", "cat{,}?", RegexOptions.None);
            yield return (@"(cat){cat}?", "cat{cat}?", RegexOptions.None);
            yield return (@"(cat){cat,5}?", "cat{cat,5}?", RegexOptions.None);
            yield return (@"(cat){5,dog}?", "cat{5,dog}?", RegexOptions.None);
            yield return (@"(cat){cat,dog}?", "cat{cat,dog}?", RegexOptions.None);
            yield return (@"()", "cat", RegexOptions.None);
            yield return (@"(?<cat>)", "cat", RegexOptions.None);
            yield return (@"(?'cat')", "cat", RegexOptions.None);
            yield return (@"(?:)", "cat", RegexOptions.None);
            yield return (@"(?imn)", "cat", RegexOptions.None);
            yield return (@"(?imn)cat", "(?imn)cat", RegexOptions.None);
            yield return (@"(?=)", "cat", RegexOptions.None);
            yield return (@"(?<=)", "cat", RegexOptions.None);
            yield return (@"(?>)", "cat", RegexOptions.None);
            yield return (@"(?()|)", "(?()|)", RegexOptions.None);
            yield return (@"(?(cat)|)", "cat", RegexOptions.None);
            yield return (@"(?(cat)|)", "dog", RegexOptions.None);
            yield return (@"(?(cat)catdog|)", "catdog", RegexOptions.None);
            yield return (@"(?(cat)catdog|)", "dog", RegexOptions.None);
            yield return (@"(?(cat)dog|)", "dog", RegexOptions.None);
            yield return (@"(?(cat)dog|)", "cat", RegexOptions.None);
            yield return (@"(?(cat)|catdog)", "cat", RegexOptions.None);
            yield return (@"(?(cat)|catdog)", "catdog", RegexOptions.None);
            yield return (@"(?(cat)|dog)", "dog", RegexOptions.None);
            yield return ("([\u0000-\uFFFF-[azAZ09]]|[\u0000-\uFFFF-[^azAZ09]])+", "azAZBCDE1234567890BCDEFAZza", RegexOptions.None);
            yield return ("[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[a]]]]]]+", "abcxyzABCXYZ123890", RegexOptions.None);
            yield return ("[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[a]]]]]]]+", "bcxyzABCXYZ123890a", RegexOptions.None);
            yield return ("[\u0000-\uFFFF-[\\p{P}\\p{S}\\p{C}]]+", "!@`';.,$+<>=\x0001\x001FazAZ09", RegexOptions.None);
            yield return (@"[\uFFFD-\uFFFF]+", "\uFFFC\uFFFD\uFFFE\uFFFF", RegexOptions.IgnoreCase);
            yield return (@"[\uFFFC-\uFFFE]+", "\uFFFB\uFFFC\uFFFD\uFFFE\uFFFF", RegexOptions.IgnoreCase);
            yield return (@"([a*]*)+?$", "ab", RegexOptions.None);
            yield return (@"(a*)+?$", "b", RegexOptions.None);
        }
    }
}
