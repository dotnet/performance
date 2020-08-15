// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using System.IO;

namespace System.Text.Experimental
{
    public class Perf_Utf8String_Base
    {
        protected string ascii_11;
        protected Utf8String ascii_11_ustring;
        protected string nonascii_110;
        protected Utf8String nonascii_110_ustring;
        protected string nonascii_chinese;
        protected Utf8String nonascii_chinese_ustring;
        protected string nonascii_cyrillic;
        protected Utf8String nonascii_cyrillic_ustring;
        protected string nonascii_greek;
        protected Utf8String nonascii_greek_ustring;

        [GlobalSetup]
        public void Setup()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "libraries", "System.Utf8String.Experimental");
            ascii_11 = File.ReadAllText(Path.Combine(path, "11.txt"));
            ascii_11_ustring = new Utf8String(ascii_11);
            nonascii_110 = File.ReadAllText(Path.Combine(path, "11-0.txt"));
            nonascii_110_ustring = new Utf8String(nonascii_110);
            nonascii_chinese = File.ReadAllText(Path.Combine(path, "25249-0.txt"));
            nonascii_chinese_ustring = new Utf8String(nonascii_chinese);
            nonascii_cyrillic = File.ReadAllText(Path.Combine(path, "30774-0.txt"));
            nonascii_cyrillic_ustring = new Utf8String(nonascii_cyrillic);
            nonascii_greek = File.ReadAllText(Path.Combine(path, "39251-0.txt"));
            nonascii_greek_ustring = new Utf8String(nonascii_greek);
        }
    }
}
