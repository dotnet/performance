using System.Collections.Generic;
using System.Globalization;
using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public class Perf_String
    {
        public static IEnumerable<object> TestStringSizes()
        {
            yield return new StringArguments(100);
            yield return new StringArguments(1000);
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public char[] GetChars(StringArguments size) // the argument is called "size" to keep the old benchmark ID, do NOT rename it
            => size.TestString1.ToCharArray();
        
        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public string Concat_str_str(StringArguments size)
            => string.Concat(size.TestString1, size.TestString2);

        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public string Concat_str_str_str(StringArguments size)
            => string.Concat(size.TestString1, size.TestString2, size.TestString3);

        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public string Concat_str_str_str_str(StringArguments size)
            => string.Concat(size.TestString1, size.TestString2, size.TestString3, size.TestString4);

        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public bool Contains(StringArguments size)
            => size.TestString1.Contains(size.Q3);

        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public bool StartsWith(StringArguments size)
            => size.TestString1.StartsWith(size.Q1);

        [Benchmark]
        [Arguments("")]
        [Arguments("TeSt!")]
        [Arguments("dzsdzsDDZSDZSDZSddsz")]
        public int GetHashCode(string s)
            => s.GetHashCode();
        
        [Benchmark]
        [Arguments("Test", 2, " Test")]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7, "Test")]
        public string Insert(string s1, int i, string s2)
            => s1.Insert(i, s2);

        [Benchmark]
        [Arguments(18)]
        [Arguments(2142)]
        public string PadLeft(int n)
            => "a".PadLeft(n);

        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10)]
        public string Remove_Int(string s, int i)
            => s.Remove(i);

        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0, 8)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7, 4)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10, 1)]
        public string Remove_IntInt(string s, int i1, int i2)
            => s.Remove(i1, i2);

        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10)]
        public string Substring_Int(string s, int i)
            => s.Substring(i);

        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0, 8)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7, 4)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10, 1)]
        public string Substring_IntInt(string s, int i1, int i2)
            => s.Substring(i1, i2);
        
        [Benchmark]
        [Arguments("A B C D E F G H I J K L M N O P Q R S T U V W X Y Z", new char[] { ' ' }, StringSplitOptions.None)]
        [Arguments("A B C D E F G H I J K L M N O P Q R S T U V W X Y Z", new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)]
        [Arguments("ABCDEFGHIJKLMNOPQRSTUVWXYZ", new char[]{' '}, StringSplitOptions.None)]
        [Arguments("ABCDEFGHIJKLMNOPQRSTUVWXYZ", new char[]{' '}, StringSplitOptions.RemoveEmptyEntries)]
        public string[] Split(string s, char[] arr, StringSplitOptions options)
            => s.Split(arr, options);

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

        [Benchmark]
        [Arguments("This is a very nice sentence", 'z', 'y')] // 'z' does not exist in the string
        [Arguments("This is a very nice sentence", 'i', 'I')] // 'i' occuress 3 times in the string
        public string Replace_Char(string text, char oldChar, char newChar)
            => text.Replace(oldChar, newChar);

        [Benchmark]
        [Arguments("This is a very nice sentence", "bad", "nice")] // there are no "bad" words in the string
        [Arguments("This is a very nice sentence", "nice", "bad")] // there are is one "nice" word in the string
        public string Replace_String(string text, string oldValue, string newValue)
            => text.Replace(oldValue, newValue);

        private static readonly string s_longString =
            "<param name=\"source\" value=\"ClientBin/OlympicsModule.xap\"><param name=\"onError\" value=\"OlySLErr\"><param name=\"background\" value=\"#00000000\"><param name=\"EnableHtmlAccess\" value=\"true\"><param name=\"windowless\" value=\"true\"><param name=\"initparams\" value=\"featurestorydivid=fsd,imagecarouseldivid=icdiv,nbcresultsdivid=nbcrd,marqueedivid=mdiv,menudivid=menudiv,resultsmessagedivid=sspo,sidebannerdivid=imgtitle\"><param name=\"maxFrameRate\" value=\"24\">\n<div id=\"olympics\" class=\"parent chrome1 triple1 cf\"><div id=\"imgtitle\" class=\"child c1\"><div class=\"child c1\"><div class=\"linkedimg\"><a href=\"http://www.nbcolympics.com/\"><img src=\"http://stb.msn.com/i/2F/B52A4C747D7C3DD651A56484793236.gif\" alt=\"Beijing 2008 Olympic Games\" width=\"42\" height=\"154\"></a></div></div></div><div id=\"hdlines\" class=\"child c1\"><div id=\"fsd\" class=\"child c1\"><div style=\"display: block;\" id=\"oslide1\" class=\"slide first\"><div class=\"photolistset\"><div class=\"photo\" id=\"ophotohalf1\"><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\"><img src=\"http://stb.msn.com/i/6A/CAA38651B85C2F342E8915D33893E.jpg\" alt=\"Ian Crocker (L) &amp; Michael Phelps (\u00A9 Al Bello/Getty \r\nImages)\" width=\"122\" height=\"110\"></a></div><div class=\"list\" id=\"olisthalf1\"><h4><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\">Familiar \nFoes</a></h4><div><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\">Phelps \nfaces rival Crocker in tonight's 100m butterfly final</a></div><ul class=\"linklist16 lstBlk\"><li class=\"first\"><a href=\"http://www.nbcolympics.com/swimming/news/newsid=219455.html?GT1=39003\">Phelps \ndenies doping allegations</a></li><li class=\"last\"><a href=\"http://www.nbcolympics.com/video/share.html?videoid=0815_hd_swb_hl_l0567&amp;GT1=39003\">Video: \nSixth gold for Phelps</a></li></ul></div></div></div><div id=\"oslide2\" class=\"slide\"><div class=\"photolistset\"><div class=\"photo\" id=\"ophotohalf2\"><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\"><img src=\"http://stc.msn.com/br/hp/en-us/css/55/i/t.gif?http://stb.msn.com/i/FE/9EE79458BEFB2816B24E639497C.jpg\" alt=\"Crocker (L) &amp; Phelps (\u00A9 Al Bello/Getty \r\nImages)\" width=\"206\" height=\"155\"></a></div><div class=\"list\" id=\"olisthalf2\"><h4><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\">Familiar \nFoes</a></h4><div><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\">Michael \nPhelps, right, will face world-record holder Ian Crocker in the 100-meter \nbutterfly final in Beijing.</a></div><ul class=\"linklist16 lstBlk\"><li class=\"first\"><a href=\"http://www.nbcolympics.com/newscenter/index.html?GT1=39003\">Latest \nnews</a></li><li class=\"last\"><a href=\"http://www.nbcolympics.com/video/index.html?GT1=39003\">Video \nhighlights</a></li></ul></div></div></div><div style=\"display: none;\" id=\"oslide3\" class=\"slide\"><div class=\"photolistset\"><div class=\"photo\" id=\"ophotohalf3\"><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\"><img src=\"http://stb.msn.com/i/6A/CAA38651B85C2F342E8915D33893E.jpg\" alt=\"Ian Crocker (L) &amp; Michael Phelps (\u00A9 Al Bello/Getty \r\nImages)\" width=\"122\" height=\"110\"></a></div><div class=\"list\" id=\"olisthalf3\"><h4><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\">Familiar \nFoes</a></h4><div><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\">Phelps \nfaces rival Crocker in tonight's 100m butterfly final</a></div><ul class=\"linklist16 lstBlk\"><li class=\"first\"><a href=\"http://www.nbcolympics.com/swimming/news/newsid=219455.html?GT1=39003\">Phelps \ndenies doping allegations</a></li><li class=\"last\"><a href=\"http://www.nbcolympics.com/video/share.html?videoid=0815_hd_swb_hl_l0567&amp;GT1=39003\">Video: \nSixth gold for Phelps</a></li></ul></div></div></div><div id=\"oslide4\" class=\"slide\"><div class=\"photolistset\"><div class=\"photo\" id=\"ophotohalf4\"><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\"><img src=\"http://stc.msn.com/br/hp/en-us/css/55/i/t.gif?http://stb.msn.com/i/FE/9EE79458BEFB2816B24E639497C.jpg\" alt=\"Crocker (L) &amp; Phelps (\u00A9 Al Bello/Getty \r\nImages)\" width=\"206\" height=\"155\"></a></div><div class=\"list\" id=\"olisthalf4\"><h4><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\">Familiar \nFoes</a></h4><div><a href=\"http://www.nbcolympics.com/swimming/news/newsid=105694.html?GT1=39003\">Michael \nPhelps, right, will face world-record holder Ian Crocker in the 100-meter \nbutterfly final in Beijing.</a></div><ul class=\"linklist16 lstBlk\"><li class=\"first\"><a href=\"http://www.nbcolympics.com/newscenter/index.html?GT1=39003\">Latest \nnews</a></li><li class=\"last\"><a href=\"http://www.nbcolympics.com/video/index.html?GT1=39003\">Video \nhighlights</a></li></ul></div></div></div></div></div><div style=\"display: block;\" id=\"hspo\" class=\"child c3\"><input class=\"button\" value=\"Hide Results\" type=\"submit\"><div class=\"results\" id=\"nbcrd\"><h3>Latest News From NBCOlympics.com</h3><ul class=\"linklist16\"><li class=\"first\"><a href=\"http://www.nbcolympics.com/trackandfield/news/newsid=218940.html?_source=rss&amp;cid=\">Surprise \nU.S. medal as track begins</a></li><li><a href=\"http://www.nbcolympics.com/gymnastics/news/newsid=216969.html?_source=rss&amp;cid=\">Inside \nthe art of Liukin's epic gold</a></li><li><a href=\"http://www.nbcolympics.com/tennis/news/newsid=217846.html?_source=rss&amp;cid=\">Blake \nloses, blasts opponent's ethics</a></li><li class=\"last\"><a href=\"http://www.nbcolympics.com/baseball/news/newsid=216758.html?_source=rss&amp;cid=\">U.S. \nfumes after loss to Cuba</a></li></ul></div></div><div style=\"display: none;\" id=\"sspo\" class=\"child c4\"><div><form action=\"#\" method=\"get\"><p>#</p><div><input class=\"button\" value=\"Show Results\" type=\"submit\"><span>Click to see up-to-the-minute news \n&amp; results from NBCOlympics.com</span></div></form></div></div><div id=\"icdiv\" class=\"child c5\"><h3><a href=\"http://www.nbcolympics.com/video/index.html?GT1=39003\">Exclusive \nVideo</a></h3><ul class=\"imglinkabslist1 cf\"><li class=\"first\"><a href=\"http://www.nbcolympics.com/video/player.html?assetid=0815_hd_gaw_hl_l1664&amp;channelcode=sportga&amp;GT1=39003\" class=\"first\"><img src=\"http://stb.msn.com/i/DF/BBBDB250C038121B9A33A96EE85B.jpg\" alt=\"Nastia Liukin (\u00A9 NBCOlympics.com)\" width=\"60\" height=\"60\"></a><a href=\"http://www.nbcolympics.com/video/player.html?assetid=0815_hd_gaw_hl_l1664&amp;channelcode=sportga&amp;GT1=39003\">Nastia \nNails It</a><p><a href=\"http://www.nbcolympics.com/video/player.html?assetid=0815_hd_gaw_hl_l1664&amp;channelcode=sportga&amp;GT1=39003\">See \nLiukin's routines</a></p></li><li><a href=\"http://www.nbcolympics.com/video/player.html?assetid=0815_hd_bvb_hl_l0516&amp;channelcode=sportbv&amp;GT1=39003\" class=\"first\"><img src=\"http://stb.msn.com/i/F7/4DB97952CB66E84B51FF71F59B8E2.jpg\" alt=\"Kerri Walsh (\u00A9 Carlos Barria/Reuters)\" width=\"60\" height=\"60\"></a><a href=\"http://www.nbcolympics.com/video/player.html?assetid=0815_hd_bvb_hl_l0516&amp;channelcode=sportbv&amp;GT1=39003\">Beach \nVolleyball</a><p><a href=\"http://www.nbcolympics.com/video/player.html?assetid=0815_hd_bvb_hl_l0516&amp;channelcode=sportbv&amp;GT1=39003\">Misty, \nKerri survive scare</a></p></li><li class=\"last\"><a href=\"http://www.nbcolympics.com/video/player.html?assetid=0814_sd_bxb_mn_l0425&amp;GT1=39003\" class=\"first\"><img src=\"http://stb.msn.com/i/D9/289343F9138E28F969B58799CD.jpg\" alt=\"Demetrius Andrade (\u00A9 Murad Sezer/AP)\" width=\"60\" height=\"60\"></a><a href=\"http://www.nbcolympics.com/video/player.html?assetid=0814_sd_bxb_mn_l0425&amp;GT1=39003\">Boxing</a><p><a href=\"http://www.nbcolympics.com/video/player.html?assetid=0814_sd_bxb_mn_l0425&amp;GT1=39003\">Biggest \nhits from Day 6</a></p></li></ul></div><div id=\"upsell\" class=\"child c6\"><div class=\"link\"><a href=\"http://www.microsoft.com/silverlight/resources/install.aspx?v=2.0%20\">For an \nenhanced Olympics experience, download Microsoft Silverlight \n2</a></div></div></div><div id=\"olyrl\" class=\"child c2 double2\"><div id=\"menudiv\" class=\"child c1\"><ul class=\"linklist1\"><li class=\"first\"><a href=\"http://www.nbcolympics.com/resultsandschedules/index.html?GT1=39003\">Schedule</a></li><li><a href=\"http://www.nbcolympics.com/medals/index.html?GT1=39003\">Medal \nCount</a></li><li><a href=\"http://www.nbcolympics.com/video/index.html?GT1=39003\">Video</a></li><li><a href=\"http://www.nbcolympics.com/athletes/index.html?GT1=39003\">Athletes</a></li><li class=\"last\"><a href=\"http://www.nbcolympics.com/photos/index.html?GT1=39003\">Photos</a></li></ul></div><div id=\"mdiv\" class=\"child c2\"><ul class=\"linklist1\"><li class=\"first\"><a href=\"http://www.nbcolympics.com/tv_and_online_listings/index.html?GT1=39003\">Olympics \nTV listings</a></li><li><a href=\"http://www.nbcolympics.com/medals/alltime/index.html?GT1=39003\">All-time \nmedal standings</a></li><li><a href=\"http://www.nbcolympics.com/photos/mostpopular/index.html?GT1=39003\">Photos: \nMost popular galleries</a></li><li><a href=\"http://www.nbcolympics.com/video/highlights/index.html?GT1=39003\">Video: \nLatest Olympic highlights</a></li><li><a href=\"http://www.nbcolympics.com/tv_and_online_listings/index.html?GT1=39003\">Olympics \nTV listings</a></li><li><a href=\"http://www.nbcolympics.com/medals/alltime/index.html?GT1=39003\">All-time \nmedal standings</a></li><li><a href=\"http://www.nbcolympics.com/photos/mostpopular/index.html?GT1=39003\">Photos: \nMost popular galleries</a></li><li><a href=\"http://www.nbcolympics.com/video/highlights/index.html?GT1=39003\">Video: \nLatest Olympic highlights</a></li><li><a href=\"http://www.nbcolympics.com/tv_and_online_listings/index.html?GT1=39003\">Olympics \nTV listings</a></li><li><a href=\"http://www.nbcolympics.com/medals/alltime/index.html?GT1=39003\">All-time \nmedal standings</a></li><li><a href=\"http://www.nbcolympics.com/photos/mostpopular/index.html?GT1=39003\">Photos: \nMost popular galleries</a></li><li class=\"last\"><a href=\"http://www.nbcolympics.com/video/highlights/index.html?GT1=39003\">Video: \nLatest Olympic highlights</a></li></ul></div></div>";

        private static readonly string s_tagName = "<foobar";
        
        [Benchmark]
        [Arguments(StringComparison.CurrentCultureIgnoreCase)]
        [Arguments(StringComparison.Ordinal)]
        [Arguments(StringComparison.OrdinalIgnoreCase)]
        public int IndexOf(StringComparison options)
            => s_longString.IndexOf(s_tagName, options);

        [Benchmark]
        [Arguments(StringComparison.CurrentCultureIgnoreCase)]
        [Arguments(StringComparison.Ordinal)]
        [Arguments(StringComparison.OrdinalIgnoreCase)]
        public int LastIndexOf(StringComparison options)
            => s_longString.LastIndexOf(s_tagName, options);

        private CultureInfo _cultureInfo;
        
        [Benchmark]
        [Arguments("The quick brown fox", "The quick brown fox")]
        [Arguments("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x")]
        public int Compare_Culture_invariant(string s1, string s2)
            => CultureInfo.InvariantCulture.CompareInfo.Compare(s1, s2, CompareOptions.None);

        [GlobalSetup(Target = nameof(Compare_Culture_en_us))]
        public void SetupCompare_Culture_en_us() => _cultureInfo = new CultureInfo("en-us");

        [Benchmark]
        [Arguments("The quick brown fox", "The quick brown fox")]
        [Arguments("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x")]
        public int Compare_Culture_en_us(string s1, string s2)
            => _cultureInfo.CompareInfo.Compare(s1, s2, CompareOptions.None);

        [GlobalSetup(Target = nameof(Compare_Culture_ja_jp))]
        public void SetupCompare_Culture_ja_jp() => _cultureInfo = new CultureInfo("ja-jp");

        [Benchmark]
        [Arguments("The quick brown fox", "The quick brown fox")]
        [Arguments("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x")]
        public int Compare_Culture_ja_jp(string s1, string s2)
            => _cultureInfo.CompareInfo.Compare(s1, s2, CompareOptions.None);

        [Benchmark]
        [Arguments(new [] {"The quick brown fox", "THE QUICK BROWN FOX"}, StringComparison.CurrentCultureIgnoreCase)]
        [Arguments(new [] {"The quick brown fox", "THE QUICK BROWN FOX"}, StringComparison.Ordinal)]
        [Arguments(new [] {"The quick brown fox", "THE QUICK BROWN FOX"}, StringComparison.OrdinalIgnoreCase)]
        [Arguments(new [] {"Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick BROWN f\u00f2x"}, StringComparison.CurrentCultureIgnoreCase)]
        [Arguments(new [] {"Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick BROWN f\u00f2x"}, StringComparison.Ordinal)]
        [Arguments(new [] {"Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick BROWN f\u00f2x"}, StringComparison.OrdinalIgnoreCase)]
        public int Compare(string[] strings, StringComparison comparison) // we should have two separate string arguments but we keep it that way to don't change the ID of the benchmark
            => string.Compare(strings[0], strings[1], comparison);
        
        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", "dzsdzsDDZSDZSDZSddsz")]
        public bool Equality(string s1, string s2)
            => s1 == s2;

        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", "dzsdzsDDZSDZSDZSddsz")]
        public bool Equals(string s1, string s2)
            => s1.Equals(s2);

        [Benchmark]
        [Arguments("Testing {0}, {0:C}, {0:D5}, {0:E} - {0:F4}{0:G}{0:N}  {0:X} !!", 8)]
        [Arguments("Testing {0}, {0:C}, {0:E} - {0:F4}{0:G}{0:N} , !!", 3.14159)]
        public string Format_OneArg(string s, object o)
            => string.Format(s, o);

        [Benchmark]
        public string Format_MultipleArgs()
            => string.Format("More testing: {0} {1} {2} {3} {4} {5}{6} {7}", '1', "Foo", "Foo", "Foo", "Foo", "Foo", "Foo", "Foo");

        [Benchmark]
        [Arguments('1', "Foo")]
        public string Interpolation_MultipleArgs(char c, string s)
            => $"More testing: {c} {s} {s} {s} {s} {s}{s} {s}"; 

        [Benchmark]
        [Arguments("TeSt")]
        [Arguments("TEST")]
        [Arguments("test")]
        public string ToUpper(string s)
            => s.ToUpper();

        [Benchmark]
        [Arguments("TeSt")]
        [Arguments("TEST")]
        [Arguments("test")]
        public string ToUpperInvariant(string s)
            => s.ToUpperInvariant();
        
        [Benchmark]
        [Arguments("TeSt")]
        [Arguments("TEST")]
        [Arguments("test")]
        public string ToLower(string s)
            => s.ToLower();

        [Benchmark]
        [Arguments("TeSt")]
        [Arguments("TEST")]
        [Arguments("test")]
        public string ToLowerInvariant(string s)
            => s.ToLowerInvariant();

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

#if NETCOREAPP2_1
        [Benchmark]
        [Arguments("This is a very nice sentence", "bad", StringComparison.CurrentCultureIgnoreCase)]
        [Arguments("This is a very nice sentence", "bad", StringComparison.Ordinal)]
        [Arguments("This is a very nice sentence", "bad", StringComparison.OrdinalIgnoreCase)]
        [Arguments("This is a very nice sentence", "nice", StringComparison.CurrentCultureIgnoreCase)]
        [Arguments("This is a very nice sentence", "nice", StringComparison.Ordinal)]
        [Arguments("This is a very nice sentence", "nice", StringComparison.OrdinalIgnoreCase)]
        public bool Contains(String text, String value, StringComparison comparisonType)
            => text.Contains(value, comparisonType);
#endif
    }
    
    public class StringArguments
    {
        public int Size { get; }

        public string TestString1 { get; }
        public string TestString2 { get; }
        public string TestString3 { get; }
        public string TestString4 { get; }

        public string Q1 { get; }
        public string Q3 { get; }

        public override string ToString() => Size.ToString(); // this argument replaced an int argument called size

        public StringArguments(int size)
        {
            Size = size;

            TestString1 = PerfUtils.CreateString(size);
            TestString2 = PerfUtils.CreateString(size);
            TestString3 = PerfUtils.CreateString(size);
            TestString4 = PerfUtils.CreateString(size);

            Q1 = TestString1.Substring(0, TestString1.Length / 4);
            Q3 = TestString1.Substring(TestString1.Length / 2, TestString1.Length / 4);
        }
    }
}