// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using System.IO;

namespace System.Text
{
    public class Perf_TextBase
    {
        // Location of the text files during runtime
        protected readonly string TextFilesRootPath = Path.Combine(Environment.CurrentDirectory, "libraries", "Common");

        // The benchmark uses text files from Project Gutenberg
        public enum InputFile
        {
            EnglishAllAscii, // English, all-ASCII, should stay entirely within fast paths
            EnglishMostlyAscii, // English, mostly ASCII with some rare non-ASCII chars, exercises that the occasional non-ASCII char doesn't kill our fast paths
            Chinese, // Chinese, exercises 3-byte scalar processing paths typical of East Asian languages
            Cyrillic, // Cyrillic, exercises a combination of ASCII and 2-byte scalar processing paths
            Greek, // Greek, similar to the Cyrillic case but with a different distribution of ASCII and non-ASCII chars
        }

        [ParamsAllValues] // BDN uses all values of given enum
        public InputFile Input { get; set; }
    }
}
